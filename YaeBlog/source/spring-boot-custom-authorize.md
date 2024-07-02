---
title: SpringBoot自定义注解实现权限控制
tags:
  - 技术笔记
  - Java
date: 2023-07-29 15:20:02
---


# SpringBoot自定义注解实现权限控制

<!--more-->

## 需求分析

最近在用`SpringBoot`写一个数据结构的大作业，实现一个日程管理系统。在系统需要有不同用户的权限管理功能。但是`SpringBoot`框架中使用的`Spring Security`权限控制框架有点过于“重”了，对于我们这种小项目来说不太适用。

于是我们打算利用`Spring`中比较强大的注解功能自行设计实现一套权限控制功能。这个功能需要实现一下这些功能：

- 基于`Json Web Token`的登录状态维持。
- 不同的用户具有不同的权限
- 使用注解注明每个接口设置的权限类型
- 在控制器中可以获得当前请求用户的身份信息

## 具体实现

### 权限等级和策略等级

首先明确一下我们需要支持的权限等级，通过枚举的方式表现：

```java
public enum UserPermission {
    USER(0, "user"),
    ADMIN(1, "administrator"),
    SUPER(2, "superman");

    private final int code;
    private final String name;

    UserPermission(int code, String name) {
        this.code = code;
        this.name = name;
    }


    public int getCode() {
        return code;
    }

    public String getName() {
        return name;
    }
}

```

这里我们使用了经典的三层等级制度。

然后明确一下我们对于各个接口的权限要求，这里也采用枚举的方式给出：

```java
public enum AuthorizePolicy {
    /**
     * 只要登录就可以访问
     */
    ONLY_LOGIN("onlyLogin"),
    /**
     * 用户在当前请求的组织中
     */
    CURRENT_GROUP_USER("currentGroupUser"),
    /**
     * 用户在当前请求的组织中 且权限在管理员之上
     */
    CURRENT_GROUP_ADMINISTRATOR("currentGroupAdministrator"),
    /**
     * 用户在当前请求的组织中 且权限在超级管理员之上
     */
    CURRENT_GROUP_SUPERMAN("currentGroupSuperman"),
    /**
     * 当前用户可以访问（URL终结点包含用户ID）
     */
    CURRENT_USER("currentUser"),
    /**
     * 用户权限超过普通管理员
     */
    ABOVE_ADMINISTRATOR("aboveAdministrator"),
    /**
     * 用户权限超过超级管理员
     */
    ABOVE_SUPERMAN("aboveSuperman");

    private final String implementName;

    AuthorizePolicy(String implementName) {
        this.implementName = implementName;
    }

    public String getImplementName() {
        return this.implementName;
    }
}

```

这里`current`开头相关策略有点粪的一点是，为了确保每个用户只能访问自己相关的数据，我们会从请求`url`中读取当前请求对象的ID。因为我们采用了`RESTful API`的设计思想，因此形如`/user/1`之类的请求就表示对于ID等于1的用户进行请求。但是这样设计就存在两个问题：

- 从编程的角度出发，`current`相关策略就只能使用在`url`最后为对象ID的接口上，但是这是一个**口头约定**，实际代码中没有任何限制，因此当错误使用这类策略时就会引起不必要的运行时错误。
- 从应用的角度出发，从`url`中获得数据也显得有一点奇怪。

### 策略实现服务

为了实现不同策略服务，我们设计了一个认证接口：

```java
package top.rrricardo.postcalendarbackend.services;

import top.rrricardo.postcalendarbackend.dtos.UserDTO;
import top.rrricardo.postcalendarbackend.exceptions.NoIdInPathException;

public interface AuthorizeService {
    /**
     * 验证用户的权限
     * @param user 用户DTO模型
     * @param requestUri 请求的URI
     * @return 是否通过拥有权限
     */
    boolean authorize(UserDTO user, String requestUri) throws NoIdInPathException;
}
```

其中输入的两个参数分别是在令牌中解析出来的用户信息（通过`json`的形式存储在`JWT`令牌中）和当前请求的`url`。

然后针对每个策略实现一个认证服务：

![image-20230727175807814](spring-boot-custom-authorize/image-20230727175807814.png)

> 具体实现就不在这里给出。

在注入每个服务时，使用策略枚举作为服务的名称，方便后续获得该服务实例。

![image-20230727175955817](spring-boot-custom-authorize/image-20230727175955817.png)

### 注解和注解解析

首先实现一个**运行时**，**注解在方法上**的注解：

```java
import java.lang.annotation.*;

@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.METHOD)
@Documented
@Inherited
public @interface Authorize {
    AuthorizePolicy policy() default AuthorizePolicy.ONLY_LOGIN;
}

```

然后实现一个注解处理器：

```java
package top.rrricardo.postcalendarbackend.components;

import io.jsonwebtoken.JwtException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.stereotype.Component;
import org.springframework.web.method.HandlerMethod;
import org.springframework.web.servlet.HandlerInterceptor;
import top.rrricardo.postcalendarbackend.annotations.Authorize;
import top.rrricardo.postcalendarbackend.dtos.ResponseDTO;
import top.rrricardo.postcalendarbackend.dtos.UserDTO;
import top.rrricardo.postcalendarbackend.exceptions.NoIdInPathException;
import top.rrricardo.postcalendarbackend.services.JwtService;

@Component
public class AuthorizeInterceptor implements HandlerInterceptor {
    private final JwtService jwtService;
    private final AuthorizeServiceFactory authorizeServiceFactory;

    private static final ThreadLocal<UserDTO> local = new ThreadLocal<>();

    public AuthorizeInterceptor(JwtService jwtService, AuthorizeServiceFactory authorizeServiceFactory) {
        this.jwtService = jwtService;
        this.authorizeServiceFactory = authorizeServiceFactory;
    }

    @Override
    public boolean preHandle(HttpServletRequest request, HttpServletResponse response, Object handler) throws Exception {
        if (!(handler instanceof HandlerMethod handlerMethod)) {
            return true;
        }

        var method = handlerMethod.getMethod();
        var authorize = method.getAnnotation(Authorize.class);
        if (authorize == null) {
            // 没有使用注解
            // 说明不需要身份验证
            return true;
        }

        // 验证是否携带令牌
        var tokenHeader = request.getHeader(jwtService.header);
        if (tokenHeader == null || !tokenHeader.startsWith(jwtService.tokenPrefix)) {
            var responseDTO = new ResponseDTO<UserDTO>("No token provided.");
            response.setStatus(401);
            response.getWriter().println(responseDTO);

            return false;
        }

        try {
            var claims = jwtService.parseJwtToken(tokenHeader);
            var userDTO = new UserDTO(
                    claims.get("userId", Integer.class),
                    claims.getIssuer(),
                    claims.get("emailAddress", String.class)
            );
            var authService = authorizeServiceFactory.getAuthorizeService(authorize.policy());

            if (authService.authorize(userDTO, request.getRequestURI())) {
                local.set(userDTO);
                return true;
            } else {
                var responseDTO = new ResponseDTO<UserDTO>("No permission");

                response.setStatus(403);
                response.getWriter().println(responseDTO);
                return false;
            }
        } catch (JwtException e) {
            // 解析令牌失败
            var responseDTO = new ResponseDTO<UserDTO>(e.getMessage());

            response.setStatus(401);
            response.getWriter().println(responseDTO);

            return false;
        } catch (NoIdInPathException e) {
            // 在请求路径中没有获取到用户ID
            var responseDTO = new ResponseDTO<UserDTO>("Internal server error, please contact administrator");

            response.setStatus(500);
            response.getWriter().println(responseDTO);

            return false;
        }
    }

    @Override
    public void afterCompletion(HttpServletRequest request, HttpServletResponse response, Object handler, Exception ex) {
        local.remove();
    }

    public static UserDTO getUserDTO() {
        return local.get();
    }
}
```

其中重要的部分是声明了一个`ThreadLocal`的变量，当前的处理线程可以通过这个变量获得当前发起请求的用户信息。

在上述实现中还涉及到使用`AuthorizeServiceFactory`的部分，这是因为在配置注解处理器时不能使用依赖注入，需要手动创建对象：

```java
import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.config.annotation.InterceptorRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;
import top.rrricardo.postcalendarbackend.components.AuthorizeInterceptor;
import top.rrricardo.postcalendarbackend.components.AuthorizeServiceFactory;
import top.rrricardo.postcalendarbackend.services.JwtService;

@Configuration
public class WebMvcConfiguration implements WebMvcConfigurer {
    private final JwtService jwtService;
    private final AuthorizeServiceFactory authorizeServiceFactory;

    public WebMvcConfiguration(JwtService jwtService, AuthorizeServiceFactory authorizeServiceFactory) {
        this.jwtService = jwtService;
        this.authorizeServiceFactory = authorizeServiceFactory;
    }

    @Override
    public void addInterceptors(InterceptorRegistry registry) {
        registry.addInterceptor(new AuthorizeInterceptor(jwtService, authorizeServiceFactory));
    }
}
```

我们就实现了一个`AuthorizeServiceFactory`，在解决依赖注入问题的同时封装了一部分的逻辑：

```java
import org.springframework.stereotype.Component;
import top.rrricardo.postcalendarbackend.enums.AuthorizePolicy;
import top.rrricardo.postcalendarbackend.services.AuthorizeService;

import java.util.Map;

@Component
public class AuthorizeServiceFactory {
    Map<String, AuthorizeService> authorizeServiceMap;

    public AuthorizeServiceFactory(Map<String, AuthorizeService> authorizeServiceMap) {
        this.authorizeServiceMap = authorizeServiceMap;
    }

    public AuthorizeService getAuthorizeService(AuthorizePolicy policy) {
        return authorizeServiceMap.get(policy.getImplementName());
    }
}
```

## 实际引用

我们在项目[PostCalendarBackend](https://github.com/post-guard/PostCalendarBackend)中实际使用的这个技术，相关代码在可以在Github上获取。

