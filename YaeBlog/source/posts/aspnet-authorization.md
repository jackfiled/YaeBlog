---
title: 在ASP.NET Core中集成认证和授权流程
date: 2024-09-08T22:27:17.0328669+08:00
tags:
- ASP.NET Core
- 技术笔记
---

以[Martina](https://github.com/post-guard/Martina)为例，记录如何典型的ASP.NET Core应用中集成认证和授权的流程。

<!--more-->

## 业务需求概述

[Martina](https://github.com/post-guard/Martina)系统是一个酒店的空调和入住管理系统，项目中对于认证和授权的要求是一个典型的多权限、多用户模式，具体来说：

- 系统中所有的接口均需要在登录之后才能调用；
- 系统中安装不同管理领域将用户的权限划分为一大类、三小类：一个超级管理员权限和客房、空调、账单三个领域管理员权限；
- 普通用户的权限有时间和使用房间的要求：只能在入住时间段内访问入住房间的空调相关接口。

可以看出，上述这些要求基本上覆盖了一个常见系统的中所有关于认证和授权的使用场景，因此本篇便以该系统为例介绍如何在ASP.NET Core框架中实现上述业务要求。

## 身份认证和授权的基础知识

身份认证是指由用户提供凭据，然后将其与存储在操作系统、数据库、应用和资源中的凭据进行比较的过程。而授权过程发生在身份认证成功之后：在凭据匹配成功之后，用户身份验证成功，可执行已向其授权的操作。授权就是判断允许用户执行操作的过程。

在ASPNET.Core中，这是通过两个**中间件**，`UseAuthenication`和`UseAuthorization`来完成的，还是来看这张经典的中间件工作流程：

![ASP.NET Core 中间件管道](./aspnet-authorization/middleware-pipeline.svg)

可以看到在中间件的管道中，认证中间价将在授权中间件运行之前运行——这两个顺序是不能颠倒的，如果授权中间件在认证中间件运行之前运行，那授权中间件就无法为用户授予任何权限，所有需要权限的接口均会返回401错误码。

> 为什么我知道的如此清楚捏？
>
> 因为我真的写反过，最后还是在框架代码里面打断点才发现授权中间件拿不到用户登录的信息，当时还在GitHub的工单里面翻找相关的bug，感觉可以评选为人生十大傻逼bug之一。

概览完认证和授权之后，首先来谈谈认证。认证的基本过程就是一个开锁的过程：用户提供一个凭据，也就是钥匙，系统验证凭据的有效性，就是锁的工作。这里主要的问题就是这个钥匙的形状长什么样子，也就是凭据的表现形式。常见的凭据表现形式有`Cookies`和`JWT`两种。

`Cookies`是一种服务器发送到用户浏览器并保存在本地上的一小块文本文件，用户浏览器在保存这些文本文件之后会在每次向同一服务器发送请求时在请求体中携带一些文本文件信息。`Cookies`是一种非常古老的技术，这种技术使得无状态的HTTP协议可以记录稳定的状态信息，因此在这个技术常被应用来认证网络用户的身份。

`JWT`的全称是JSON Web Token，是一种使用JSON对象表示格式在两方之前安全且有效的传输信息的方法，使用该方法的信息可以使用指定的密钥或者是公钥-私钥对验证信息的有效性。因此`JWT`作为一种通用的、可验证的令牌格式用来完成网络中认证的过程。在服务器验证某一个用户的身份之后（例如通过验证账号密码、通过第三方的验证）可以签发一个`JWT`令牌给用户浏览器，浏览器可以使用`localstorage`等技术将该令牌存储在用户浏览器中并在每次向服务器发送请求的过程中将该令牌携带在一个特定的请求头`Authorization`中。

> 在`Authorization`请求头中常常会以`Bearer <JWT>`的格式进行，这其中的`Bearer`是指定的身份认证的模式（Scheme），这里的详细解释可以见[MDN文档](https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication)。

谈完认证之后，再来看看授权。授权的实现是一个和业务逻辑高度相关的过程，一个常见的业务逻辑是用户分为不同的层级——例如普通用户和管理员，而不同层级的用户可以调用的接口不同，这就是**基于策略的授权模式**的典型应用场景，该模式允许为每个接口指定一个或者多个认证策略。另外一个常见的业务逻辑是用户只能访问自己所拥有的资源——例如用户只能删除自己创建的记录，这就是**基于资源的授权模式**的典型应用场景，该模式允许为一种资源编写一段授权逻辑，并通过依赖注入的方式供服务器或者控制器使用。

## 身份认证和授权的实践

在本个系统中，身份认证将采用`JWT`令牌，而授权的部分将会覆盖到上文中提到的两种典型模式，通过研究本系统的实现可以理解在ASP.NET Core中集成身份认证和授权的流程。

在ASP.NET Core系统中集成`JWT`令牌的认证方式需要先安装一个包`Microsoft.AspNetCore.Authentication.JwtBearer`。

### 身份认证部分

身份认证部分主要分为令牌签发和令牌验证两个部分，令牌认证的部分主要在于使用`AddAuthentication`向主机容器中注入服务，而令牌签发的部分则通常是实现一个接口，在验证用户输入的账号和密码之后生成该用户对于的令牌。这两个过程是高度关联的，在签发过程中设置的令牌信息需要在验证令牌的过程设置对应的部分，否则签发的令牌就无法验证。因此先介绍签发令牌的部分。

签发令牌之前先介绍一下`JWT`令牌的组成，一个兼容的`JWT`令牌一般有三个部分组成：

- 头部`Header`：头部在一般情况下只有两个字段组成，一个`tpy`字段存储固定值为`JWT`指定这是一个`JWT`令牌，一个`alg`字段指定验证该令牌的算法是`HMCA SHA256`还是`RSA`：

  ```json
  {
    "alg": "HS256",
    "typ": "JWT"
  }
  ```

- 负载`Payload`：包含各种关于实体（用户）的宣称列表。宣称可以分成三种类型，已注册的类型、公开的类型和私有的类型，这三种的类型的区别可以从[RFC7519](https://datatracker.ietf.org/doc/html/rfc7519#section-4.1)中具体查看，简而言之就是已注册的类型就是推荐在签发令牌时设置的，包括签发者和到期时间等的内容，公开的类型是公开注册可以共享的名称，而私有的就是自行指定的。

  ```json
  {
    "sub": "1234567890",
    "name": "John Doe",
    "admin": true
  }
  ```

- 签名`signature`：验证令牌的签名部分，在使用`HMCA SHA256`算法的情况下，签名的计算公示如下所示：

  ```
  HMACSHA256(
    base64UrlEncode(header) + "." +
    base64UrlEncode(payload),
    secret)
  ```

在学习了这些`JWT`的基础知识之后就可以很容易的写出如下的令牌生成代码：

```csharp
	public string GenerateJsonWebToken(User user)
    {
        List<Claim> claims =
        [
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.UserId)
        ];

        JwtSecurityToken token = new(
            issuer: _option.Issuer,
            audience: user.UserId,
            notBefore: DateTime.Now,
            expires: DateTime.Now.AddDays(7),
            claims: claims,
            signingCredentials: _signingCredentials
        );

        return _jwtSecurityTokenHandler.WriteToken(token);
    }
```

签发令牌的凭据使用下面的方式创建：

```csharp
private readonly SigningCredentials _signingCredentials =
        new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jsonWebTokenOption.Value.JsonWebTokenKey)),
            SecurityAlgorithms.HmacSha256);
```

签发的过程中部分重要的参数使用配置的方式提供，例如签发者和密钥，配置实体类如下所示：

```csharp
public class JsonWebTokenOption
{
    public const string OptionName = "JWT";

    /// <summary>
    /// JWT令牌的签发者
    /// </summary>
    public required string Issuer { get; set; }

    /// <summary>
    /// JWT令牌的签发密钥
    /// </summary>
    public required string JsonWebTokenKey { get; set; }
}

```

签发好令牌之后就可以编写验证令牌的部分了：

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(
    options =>
    {
        JsonWebTokenOption? jsonWebTokenOption = builder.Configuration.GetSection(JsonWebTokenOption.OptionName)
            .Get<JsonWebTokenOption>();

        if (jsonWebTokenOption is null)
        {
            throw new InvalidOperationException("Failed to get JWT options");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jsonWebTokenOption.Issuer,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jsonWebTokenOption.JsonWebTokenKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    });

```

在验证令牌的部分，指定验证令牌的签发者和签名。

编写完上述代码之后就可以增加身份验证和授权的中间件验证上述代码的正确性了。

```csharp
application.UseAuthentication();
application.UseAuthorization();
```

### 授权的部分

#### 按照策略进行授权

系统中一个典型的场景就是不同级别的用户能访问的接口不同，例如在本系统中用户的级别分为：

```csharp
[Flags]
public enum Roles
{
    User = 0b_0000_0000,
    RoomAdministrator = 0b_0000_0001,
    AirConditionerAdministrator = 0b_0000_0010,
    BillAdministrator = 0b_0000_0100,
    Administrator = 0b_0000_1000
}
```

为了方便给不同的接口指定不同的访问策略，首先创建一个对用户级别的要求（Requirement）：

```csharp
public class HotelRoleRequirement(Roles hotelRole) : IAuthorizationRequirement
{
    public Roles HotelRole { get; } = hotelRole;
}
```

然后实现一个处理该要求的验证程序：

```csharp
public class HotelRoleHandler(MartinaDbContext dbContext) : AuthorizationHandler<HotelRoleRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        HotelRoleRequirement requirement)
    {
        Claim? userId = context.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return;
        }

        User? user = await dbContext.Users
            .Include(u => u.Permission)
            .Where(u => u.UserId == userId.Value)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return;
        }

        // 如果要求的权限是超级管理员
        // 则判断是否是超级管理员
        if ((requirement.HotelRole & Roles.Administrator) == Roles.Administrator)
        {
            if (user.Permission.IsAdministrator)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        // 剩下的权限
        // 如果用户是超级管理员则直接有权限
        if (user.Permission.IsAdministrator)
        {
            context.Succeed(requirement);
            return;
        }

        if ((requirement.HotelRole & Roles.BillAdministrator) == Roles.BillAdministrator)
        {
            if (user.Permission.BillAdminstrator)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        if ((requirement.HotelRole & Roles.RoomAdministrator) == Roles.RoomAdministrator)
        {
            if (user.Permission.RoomAdministrator)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        if ((requirement.HotelRole & Roles.AirConditionerAdministrator) == Roles.AirConditionerAdministrator)
        {
            if (user.Permission.AirConditionorAdministrator)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }
}
```

框架要求在处理程序使用依赖注入到主机的容器中，这里因为在验证的过程中使用了数据库的服务`DbContext`因此被注册为一个范围内（Scope）服务。

```csharp
builder.Services.AddScoped<IAuthorizationHandler, HotelRoleHandler>();
```

为了方便在`[Authorize]`注解中使用字符串指定不同的授权策略，在`AddAuthoriztion`进行配置：

```csharp
builder.Services.AddAuthorization(options => 
    {
         options.AddPolicy("Administrator", policy =>
        {
            policy.AddRequirements(new HotelRoleRequirement(Roles.Administrator));
        });

        options.AddPolicy("RoomAdministrator", policy =>
            policy.AddRequirements(new HotelRoleRequirement(Roles.RoomAdministrator)));

        options.AddPolicy("AirConditionerAdministrator", policy =>
            policy.AddRequirements(new HotelRoleRequirement(Roles.AirConditionerAdministrator)));

        options.AddPolicy("BillAdministrator", policy =>
            policy.AddRequirements(new HotelRoleRequirement(Roles.BillAdministrator)));
    });
```

使用该方法注册之后就可以直接在`[Authorize]`注解中指定需要使用的授权策略：

```csharp
    [HttpGet("revenue")]
    [Authorize(policy: "BillAdministrator")]
    [ProducesResponseType<ExceptionMessage>(400)]
    [ProducesResponseType<RevenueTrend>(200)]
    public async Task<IActionResult> QueryRevenueTrend([FromQuery] DateTimeOffset begin, [FromQuery] DateTimeOffset end)
    {
        if (begin >= end)
        {
            return BadRequest(new ExceptionMessage("开始时间不能晚于结束时间"));
        }

        RevenueTrend trend = new()
        {
            TotalUsers = await managerService.QueryCurrentUser(),
            TotalCheckin = await managerService.QueryCurrentCheckin(),
            DailyRevenues = await managerService.QueryDailyRevenue(begin, end)
        };

        return Ok(trend);
    }
```

#### 按照资源进行授权

系统中一个典型的需求就是一个用户只能修改资源池中部分自己拥有权限的资源，在本系统中就是用户只能开启和关闭当前入住房间中的空调。

按照资源进行授权的总体流程和安装策略进行授权总体上差别不大，除了无法在注解中设置需要使用的策略。首先仍然是设计一个授权的要求：

```csharp
public class CheckinRequirement : IAuthorizationRequirement;
```

然后为该要求实现一个授权处理程序，注意在这里集成泛型基类`AuthorizationHandler`时除了需要指定要求类还需要指定资源类型：

```csharp
public class CheckinHandler(
    RoomService roomService,
    MartinaDbContext dbContext)
    : AuthorizationHandler<CheckinRequirement, Room>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        CheckinRequirement requirement,
        Room resource)
    {
        Claim? userId = context.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return;
        }

        User? user = await dbContext.Users.AsNoTracking()
            .Where(u => u.UserId == userId.Value)
            .FirstOrDefaultAsync();

        if (user is { Permission.IsAdministrator: true } || user is { Permission.AirConditionorAdministrator: true })
        {
            context.Succeed(requirement);
            return;
        }

        CheckinRecord? record = await roomService.QueryUserCurrentStatus(userId.Value);

        if (record?.RoomId == resource.Id)
        {
            context.Succeed(requirement);
        }
    }
}
```

在使用该授权方法时，通过依赖注入获得一个`IAuthorizationService`的接口对象并调用对应的授权接口进行验证，传入需要访问的资源和当前`HttpContext`中的用户`User`，这个`User`实际上就是`JWT`令牌中的负载部分。

```csharp
		AuthorizationResult result = await authorizationService.AuthorizeAsync(User, room, [new CheckinRequirement()]);

        if (!result.Succeeded)
        {
            return Forbid();
        }

        if (!airConditionerManageService.VolidateAirConditionerRequest(roomObjectId, request, out string? message))
        {
            return BadRequest(new ExceptionMessage(message));
        }
```

## 总结

通过清晰的定义身份认证和授权两个环节，并提供了一个要求——处理程序的授权模型，ASP.NET Core提供了一套简单易用、扩展性高的接口安全系统。
