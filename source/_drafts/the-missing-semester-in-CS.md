---
title: the_missing_semester_in_CS
tags:
typora-root-url: the-missing-semester-in-CS
---

# The missing semester in the CS

## The Shell

### what is the shell?

Run programs, give them input, and inspect their output in a semi-structured way.

This course will introduce the bash, Bourne Again Shell. To run open a shell prompt(where you can type commands),  you first need a *terminal*.

> 我在Windows的Windows Subsystem for linux里学习这个玩意儿

### Using the shell

```bash
ricardo@g15:~$
```

The main textual interface to the shell. The "missing" means you are in the machine "missing", and your "current working directory". or where you currently are, is `~`(short for home).

At this prompt you can type a *command*, which will then be interpreted by the shell.

```bash
ricardo@g15:~$ date
Sun Mar 27 21:51:49 CST 2022
ricardo@g15:~$
```

The shell parses the command by splitting it by whitespace, and then runs the program indicated by the first word, supplying each subsequent word as an argument that the program can access. If you want to provide an argument that contains spaces or other special characters (like a directory named “My Photos”), you can either quote the argument with `'` or `"` (`"My Photos"`), or escape just the relevant characters with `\` (`My\ Photos`).

the shell is a programming environment, just like Python or Ruby, and so it has variables, conditionals, loops, and functions. When you run commands in your shell, you are really writing a small bit of code that your shell interprets. If the shell is asked to execute a command that doesn’t match one of its programming keywords, it consults an *environment variable* called `$PATH` that lists which directories the shell should search for programs when it is given a command:

```bash
ricardo@g15:~$ echo $PATH
/home/ricardo/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/usr/lib/wsl/lib
ricardo@g15:~$ which echo
/usr/bin/echo
ricardo@g15:~$ /usr/bin/echo $PATH
/home/ricardo/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/usr/lib/wsl/lib
```

When we run the `echo` command, the shell sees that it should execute the program `echo`, and then searches through the `:`-separated list of directories in `$PATH` for a file by that name. When it finds it, it runs it (assuming the file is *executable*; more on that later). We can find out which file is executed for a given program name using the `which` program. We can also bypass `$PATH` entirely by giving the *path* to the file we want to execute.

### Navigating in the shell

A path on the shell is a delimited(分隔的) list of directories; separated by `/` on Linux and macOS, where  the path `/` is the root of the file system, under which all directories and files lie.A path that starts with `/` is called an *absolute* path. Any other path is a *relative* path. Relative paths are relative to the current working directory, which we can see with the `pwd` command and change with the `cd` command. In a path, `.` refers to the current directory, and `..` to its parent directory.

```bash
ricardo@g15:~$ pwd
/home/ricardo
ricardo@g15:~$ cd /home
ricardo@g15:/home$ cd ..
ricardo@g15:/$ pwd
/
ricardo@g15:/$ cd ./home/
ricardo@g15:/home$ pwd
/home
ricardo@g15:/home$ cd missing
-bash: cd: missing: No such file or directory
ricardo@g15:/home$ cd ricardo/
ricardo@g15:~$ pwd
/home/ricardo
ricardo@g15:~$ ../../usr/bin/echo hello
hello
```

In general, when we run a program, it will operate in the current directory unless we tell it otherwise. For example, it will usually search for files there, and create new files there if it needs to.

To see what lives in a given directory, we use the `ls` command

```bash
ricardo@g15:~$ ls
bin  code  download  github  tmp
ricardo@g15:~$ cd ..
ricardo@g15:/home$ ls
ricardo
ricardo@g15:/home$ cd ..
ricardo@g15:/$ ls
bin  boot  build  dev  etc  home  init  lib  lib32  lib64  libx32  lost+found  media  mnt  opt  proc  root  run  sbin  snap  srv  sys  tmp  usr  var
```

Unless a directory is given as its first argument, `ls` will print the contents of the current directory. 

Most commands accept flags and options (flags with values) that start with `-` to modify their behavior. Usually, running a program with the `-h` or `--help` flag will print some help text that tells you what flags and options are available.

For example, `ls --help` tells us

```bash
  -l                         use a long listing format
```

```bash
ricardo@g15:/$ ls -l /home
total 4
drwxr-xr-x 15 ricardo ricardo 4096 Mar 25 19:14 ricardo
```

This gives us a bunch more information about each file or directory present. First, the `d` at the beginning of the line tells us that `ricardo` is a directory. Then follow three groups of three characters (`rwx`). These indicate what permissions the owner of the file (`ricardo`), the owning group (`ricardo`), and everyone else respectively have on the relevant item. A `-` indicates that the given principal does not have the given permission. Above, only the owner is allowed to modify (`w`) the `ricardo` directory (i.e., add/remove files in it). To enter a directory, a user must have “search” (represented by “execute”: `x`) permissions on that directory (and its parents). To list its contents, a user must have read (`r`) permissions on that directory. For files, the permissions are as you would expect. Notice that nearly all the files in `/bin` have the `x` permission set for the last group, “everyone else”, so that anyone can execute those programs.

Some other handy programs to know about at this point are `mv` (to rename/move a file), `cp` (to copy a file), and `mkdir` (to make a new directory).

> `mv`可以重命名大概就是把一个文件原地移动一下，移动的时候目标文件改个名（

If you ever want *more* information about a program’s arguments, inputs, outputs, or how it works in general, give the `man` program a try. It takes as an argument the name of a program, and shows you its *manual page*. Press `q` to exit.

### Connecting programs

In the shell, programs have two primary “streams” associated with them: their input stream and their output stream. When the program tries to read input, it reads from the input stream, and when it prints something, it prints to its output stream. Normally, a program’s input and output are both your terminal. That is, your keyboard as input and your screen as output. However, we can also rewire those streams!

The simplest form of redirection is `< file` and `> file`. These let you rewire the input and output streams of a program to a file respectively:

```bash
ricardo@g15:~$ echo hello > hello.txt
ricardo@g15:~$ cat hello.txt
hello
ricardo@g15:~$ cat < hello.txt
hello
ricardo@g15:~$ cat < hello.txt > hello2.txt
ricardo@g15:~$ cat hello2.txt
hello
```

Demonstrated(展示，证明) in the example above, `cat` is a program that con`cat`enates files. When given file names as arguments, it prints the contents of each of the files in sequence to its output stream. But when `cat` is not given any arguments, it prints contents from its input stream to its output stream (like in the third example above).

> 上文中的`concatenate` 是一个单词，表示连接，这里的大概意思是表示cat的含义是连接

You can also use `>>`to append to a file.

Where this kind of input/output redirection really shines is in the use of *pipes*. The `|` operator lets you “chain” programs such that the output of one is the input of another:

```bash
ricardo@g15:~$ ls -l / | tail -n1
drwxr-xr-x  13 root root   4096 Apr 23  2020 var
ricardo@g15:~$ curl --head --silent baidu.com | grep --ignore-case content-length | cut --delimiter=' ' -f2
81
```

> 在教程中是访问了google.com，我修改为baidu.com

### A versatile and powerful tool

> versatile原义是指多才多艺的

On most Unix-like systems, one user is special: the “root” user. You may have seen it in the file listings above. The root user is above (almost) all access restrictions, and can create, read, update, and delete any file in the system. You will not usually log into your system as the root user though, since it’s too easy to accidentally break something. Instead, you will be using the `sudo` command. As its name implies, it lets you “do” something “as su” (short for “super user”, or “root”). When you get permission denied errors, it is usually because you need to do something as root. Though make sure you first double-check that you really wanted to do it that way!

One thing you need to be root in order to do is writing to the `sysfs` file system mounted under `/sys`. `sysfs` exposes a number of kernel parameters as files, so that you can easily reconfigure the kernel on the fly without specialized tools.

> 这里`sysfs`"将一系列内核的参数利用文件的形式暴露出来"体现了Unix"文件就是系统“的设计思想。

> 这里教程通过在`/sys/class/backlight`下属的文件中写值的方式改变亮度，但是我Linux环境中并没有这个文件，也就是我的Linux不支持调节亮度。

>在这个例子中还提到了像`|`, `>`, `<`这类的操作是由shell程序来完成的，因此他们的执行权限仅仅是当前登录用户的权限，在写入一些需要高权限的文件是可能会出现`Permission denied`

### Exercises

1. ```bash
   ricardo@g15:~$ echo $SHELL
   /bin/bash
   ```

2. ```bash
   ricardo@g15:/tmp$ mkdir missing
   ```

3. ```bash
   ricardo@g15:/tmp$ man touch
   ```

   > touch 就是改变一个文件的最后修改时间，如果没有就是创建

4. ```bash
   ricardo@g15:/tmp$ touch missing/semester
   ```

5. ```bash
   ricardo@g15:/tmp/missing$ touch semester
   ricardo@g15:/tmp/missing$ echo '#!/bin/sh' > semester
   ricardo@g15:/tmp/missing$ echo curl --head --silent https://missing.csail.mit.edu >> semester
   ricardo@g15:/tmp/missing$ cat semester
   #!/bin/sh
   curl --head --silent https://missing.csail.mit.edu
   ```

6. ```bash
   ricardo@g15:/tmp/missing$ ./semester
   -bash: ./semester: Permission denied
   ricardo@g15:/tmp/missing$ ll
   total 32
   drwxr-xr-x 2 ricardo ricardo  4096 Mar 28 08:47 ./
   drwxrwxrwt 7 root    root    20480 Mar 28 08:38 ../
   -rw-r--r-- 1 ricardo ricardo    61 Mar 28 08:47 semester
   ```

7. ```bash
   ricardo@g15:/tmp/missing$ sh ./semester
   HTTP/2 200
   server: GitHub.com
   content-type: text/html; charset=utf-8
   last-modified: Fri, 04 Mar 2022 17:03:44 GMT
   access-control-allow-origin: *
   etag: "62224670-1f37"
   expires: Mon, 28 Mar 2022 00:59:14 GMT
   cache-control: max-age=600
   x-proxy-cache: MISS
   x-github-request-id: 3814:0F07:F7E95E:1022BDD:62410609
   accept-ranges: bytes
   date: Mon, 28 Mar 2022 00:49:14 GMT
   via: 1.1 varnish
   age: 0
   x-served-by: cache-itm18847-ITM
   x-cache: MISS
   x-cache-hits: 0
   x-timer: S1648428554.944692,VS0,VE156
   vary: Accept-Encoding
   x-fastly-request-id: e71e5760a7ed66425c9ad2eb9572e5a12b23bee6
   content-length: 7991
   ```

8. > chmod命令用于修改文件的权限

9. ```bash
   ricardo@g15:/tmp/missing$ sudo chmod 777 semester
   [sudo] password for ricardo:
   ricardo@g15:/tmp/missing$ ll
   total 32
   drwxr-xr-x 2 ricardo ricardo  4096 Mar 28 08:47 ./
   drwxrwxrwt 7 root    root    20480 Mar 28 08:38 ../
   -rwxrwxrwx 1 ricardo ricardo    61 Mar 28 08:47 semester*
   ricardo@g15:/tmp/missing$ ./semester
   HTTP/2 200
   server: GitHub.com
   content-type: text/html; charset=utf-8
   last-modified: Fri, 04 Mar 2022 17:03:44 GMT
   access-control-allow-origin: *
   etag: "62224670-1f37"
   expires: Sun, 27 Mar 2022 14:38:22 GMT
   cache-control: max-age=600
   x-proxy-cache: MISS
   x-github-request-id: 083A:17A2:291CE6:2F68B7:62407485
   fastly-original-body-size: 0
   accept-ranges: bytes
   date: Mon, 28 Mar 2022 00:51:23 GMT
   via: 1.1 varnish
   age: 0
   x-served-by: cache-hkg17932-HKG
   x-cache: HIT
   x-cache-hits: 1
   x-timer: S1648428683.136854,VS0,VE260
   vary: Accept-Encoding
   x-fastly-request-id: 8271d76f868dc9951a9dc5b5d2b1da1d1ace0e89
   content-length: 7991
   ```

10. ```bash
    ricardo@g15:/tmp/missing$ ./semester | grep --ignore-case last-modified | cut --delimiter=':' -f2 > /home/ricardo/last-modified.txt
    ricardo@g15:/tmp/missing$ cat /home/ricardo/last-modified.txt
     Fri, 04 Mar 2022 17
    ```

11. >卡牌名称：WSL系统
    >
    >卡牌效果:当遇到使用`sysfs`的题目时，打出此牌，即可跳过该回合

## Shell Tools and Scripting

### Shell Scripting

Shell scripts are the next step in complexity. Most shells have their own scripting language with variables, control flow and its own syntax. What makes shell scripting different from other scripting programming language is that it is optimized for performing shell-related tasks. Thus, creating command pipelines, saving results into files, and reading from standard input are primitives in shell scripting, which makes it easier to use than general purpose scripting languages. For this section we will focus on bash scripting since it is the most common.

> `primitive`表示原始人，原函数的意思，这里表示管道，保存到文件等等的工具是`bash`本来就有的，可以使在`bash`脚本中的工作更加顺利。

To assign variables in bash, use the syntax `foo=bar` and access the value of the variable with `$foo`. Note that `foo = bar` will not work since it is interpreted as calling the `foo` program with arguments `=` and `bar`. In general, in shell scripts the space character will perform argument splitting. This behavior can be confusing to use at first, so always check for that.

Strings in bash can be defined with `'` and `"` delimiters, but they are not equivalent. Strings delimited with `'` are literal strings and will not substitute variable values whereas `"` delimited strings will.

> `subsitute` 是“代替，替代”的意思。

```bash
ricardo@g15:~$ foo=bar
ricardo@g15:~$ echo "$foo"
bar
ricardo@g15:~$ echo '$foo'
$foo
```

> 直接用`""`就可以进行字符串内插，还是非常方便的。

As with most programming languages, bash supports control flow techniques including `if`, `case`, `while` and `for`. Similarly, `bash` has functions that take arguments and can operate with them. Bash uses a variety of special variables to refer to arguments, error codes, and other relevant variables. Below is a list of some of them.

- `$0` Name of the script
- `$1` to `$9` - Arguments to the script. `$1` is the first argument and so on.
- `$@` - All the arguments
- `$#` - Number of arguments
- `$?` - Return code of the previous command
- `$$` - Process identification number (PID) for the current script
- `!!` - Entire last command, including arguments. 
- `$_` - Last argument from the last command. 

```bash
ricardo@g15:/tmp/missing$ date
Sat Apr  2 18:11:40 CST 2022
ricardo@g15:/tmp/missing$ !!
date
Sat Apr  2 18:11:42 CST 2022
ricardo@g15:/tmp/missing$ ls -l
total 8
-rwxrwxrwx 1 ricardo ricardo 61 Mar 28 08:47 semester
-rw-r--r-- 1 ricardo ricardo 34 Apr  2 18:07 shell.sh
ricardo@g15:/tmp/missing$ echo $_
-l
```

Commands will often return output using `STDOUT`, errors through `STDERR`, and a Return Code to report errors in a more script-friendly manner. A value of 0 usually means everything went OK; anything different from 0 means an error occurred.

Exit codes can be used to conditionally execute commands using `&&` (and operator) and `||` (or operator), both of which are short-circuiting operators. Commands can also be separated within the same line using a semicolon `;`. The `true` program will always have a 0 return code and the `false` command will always have a 1 return code.

```bash
ricardo@g15:/tmp/missing$ false || echo "Oops, fail"
Oops, fail
ricardo@g15:/tmp/missing$ true || echo "Will not be printed"
ricardo@g15:/tmp/missing$ true && echo "Things went well"
Things went well
ricardo@g15:/tmp/missing$ false && echo "Will not be printed"
ricardo@g15:/tmp/missing$ true ; echo "This will always run"
This will always run
ricardo@g15:/tmp/missing$ false ; echo "This will always run"
This will always run
```

Another common pattern is wanting to get the output of a command as a variable. This can be done with *command substitution*. Whenever you place `$( CMD )` it will execute `CMD`, get the output of the command and substitute it in place. 

```bash 
ricardo@g15:/tmp/missing$ cat shell.sh
#!/bin/bash

echo "Starting program at $(date)" # Date will be substituted

echo "Running program $0 with $# arguments with pid $$"

for file in "$@"; do
    grep foobar "$file" > /dev/null 2> /dev/null
    # When pattern is not found, grep has exit status 1
    # We redirect STDOUT and STDERR to a null register since we do not care about them
    if [[ $? -ne 0 ]]; then
        echo "File $file does not have any foobar, adding one"
        echo "# foobar" >> "$file"
    fi
done
ricardo@g15:/tmp/missing$ cat temp
foobar
ricardo@g15:/tmp/missing$ ./shell.sh
Starting program at Sat Apr  2 20:20:05 CST 2022
Running program ./shell.sh with 0 arguments with pid 10569
ricardo@g15:/tmp/missing$ ./shell.sh temp
Starting program at Sat Apr  2 20:20:16 CST 2022
Running program ./shell.sh with 1 arguments with pid 10573
ricardo@g15:/tmp/missing$ echo "foo" > temp
ricardo@g15:/tmp/missing$ cat temp
foo
ricardo@g15:/tmp/missing$ ./shell.sh temp
Starting program at Sat Apr  2 20:21:15 CST 2022
Running program ./shell.sh with 1 arguments with pid 10577
File temp does not have any foobar, adding one
```

- Wildcards - Whenever you want to perform some sort of wildcard matching, you can use `?` and `*` to match one or any amount of characters respectively. For instance, given files `foo`, `foo1`, `foo2`, `foo10` and `bar`, the command `rm foo?` will delete `foo1` and `foo2` whereas `rm foo*` will delete all but `bar`.
- Curly braces `{}` - Whenever you have a common substring in a series of commands, you can use curly braces for bash to expand this automatically. This comes in very handy when moving or converting files.

Note that scripts need not necessarily be written in bash to be called from the terminal. The kernel knows to execute this script with a python interpreter instead of a shell command because we included a [shebang](https://en.wikipedia.org/wiki/Shebang_(Unix)) line at the top of the script. It is good practice to write shebang lines using the [`env`](https://www.man7.org/linux/man-pages/man1/env.1.html) command that will resolve to wherever the command lives in the system, increasing the portability of your scripts. To resolve the location, `env` will make use of the `PATH` environment variable we introduced in the first lecture. For this example the shebang line would look like `#!/usr/bin/env python`.

### Shell Tools

#### Finding how to use commands

The first-order approach is to call said command with the `-h` or `--help` flags. A more detailed approach is to use the `man` command. Short for manual, [`man`](https://www.man7.org/linux/man-pages/man1/man.1.html) provides a manual page (called manpage) for a command you specify.

Sometimes manpages can provide overly detailed descriptions of the commands, making it hard to decipher what flags/syntax to use for common use cases. [TLDR pages](https://tldr.sh/) are a nifty complementary solution that focuses on giving example use cases of a command so you can quickly figure out which options to use. 

#### Finding files

One of the most common repetitive tasks that every programmer faces is finding files or directories. All UNIX-like systems come packaged with [`find`](https://www.man7.org/linux/man-pages/man1/find.1.html), a great shell tool to find files. `find` will recursively search for files matching some criteria.

```bash
# Find all directories named src
find . -name src -type d
# Find all python files that have a folder named test in their path
find . -path '*/test/*.py' -type f
# Find all files modified in the last day
find . -mtime -1
# Find all zip files with size in range 500k to 10M
find . -size +500k -size -10M -name '*.tar.gz'
# Delete all files with .tmp extension
find . -name '*.tmp' -exec rm {} \;
# Find all PNG files and convert them to JPG
find . -name '*.png' -exec convert {} {}.jpg \;
```

For instance, [`fd`](https://github.com/sharkdp/fd) is a simple, fast, and user-friendly alternative to `find`. It offers some nice defaults like colorized output, default regex matching, and Unicode support. 

Most would agree that `find` and `fd` are good, but some of you might be wondering about the efficiency of looking for files every time versus compiling some sort of index or database for quickly searching. That is what [`locate`](https://www.man7.org/linux/man-pages/man1/locate.1.html) is for. `locate` uses a database that is updated using [`updatedb`](https://www.man7.org/linux/man-pages/man1/updatedb.1.html). In most systems, `updatedb` is updated daily via [`cron`](https://www.man7.org/linux/man-pages/man8/cron.8.html). Therefore one trade-off between the two is speed vs freshness. Moreover `find` and similar tools can also find files using attributes such as file size, modification time, or file permissions, while `locate` just uses the file name.

#### Finding code

A common scenario is wanting to search for all files that contain some pattern, along with where in those files said pattern occurs. To achieve this, most UNIX-like systems provide [`grep`](https://www.man7.org/linux/man-pages/man1/grep.1.html), a generic tool for matching patterns from the input text. 

#### Finding shell commands

The first thing to know is that typing the up arrow will give you back your last command, and if you keep pressing it you will slowly go through your shell history.

The `history` command will let you access your shell history programmatically. It will print your shell history to the standard output. If we want to search there we can pipe that output to `grep` and search for patterns. `history | grep find` will print commands that contain the substring “find”.

In most shells, you can make use of `Ctrl+R` to perform backwards search through your history. After pressing `Ctrl+R`, you can type a substring you want to match for commands in your history. As you keep pressing it, you will cycle through the matches in your history. 

Another cool history-related trick I really enjoy is **history-based autosuggestions**. First introduced by the [fish](https://fishshell.com/) shell, this feature dynamically autocompletes your current shell command with the most recent command that you typed that shares a common prefix with it. It can be enabled in [zsh](https://github.com/zsh-users/zsh-autosuggestions) and it is a great quality of life trick for your shell.

#### Directory Navigation

As with the theme of this course, you often want to optimize for the common case. Finding frequent and/or recent files and directories can be done through tools like [`fasd`](https://github.com/clvv/fasd) and [`autojump`](https://github.com/wting/autojump). Fasd ranks files and directories by [*frecency*](https://web.archive.org/web/20210421120120/https://developer.mozilla.org/en-US/docs/Mozilla/Tech/Places/Frecency_algorithm), that is, by both *frequency* and *recency*. By default, `fasd` adds a `z` command that you can use to quickly `cd` using a substring of a *frecent* directory. 

More complex tools exist to quickly get an overview of a directory structure: [`tree`](https://linux.die.net/man/1/tree), [`broot`](https://github.com/Canop/broot) or even full fledged file managers like [`nnn`](https://github.com/jarun/nnn) or [`ranger`](https://github.com/ranger/ranger).

### Exercise

1. ```bash
   ricardo@g15:~$ ls -l -h -t
   total 20K
   drwxr-xr-x 2 ricardo ricardo 4.0K Apr  5 11:42 tmp
   drwxr-xr-x 8 ricardo ricardo 4.0K Mar 24 20:11 code
   drwxr-xr-x 2 ricardo ricardo 4.0K Mar 16 21:26 bin
   drwxr-xr-x 4 ricardo ricardo 4.0K Mar 16 21:25 download
   drwxr-xr-x 3 ricardo ricardo 4.0K Jan 21 18:21 github
   ```

2. 直接在`.bashrc`文件里面添加

   ```bash
   path=/home/ricardo
   
   marco () {
     path=$(pwd)
   }
   
   polo () {
     cd $path
   }
   ```

   测试一下

   ```bash
   ricardo@g15:~/tmp$ marco
   ricardo@g15:~/tmp$ cd ..
   ricardo@g15:~$ polo
   ricardo@g15:~/tmp$
   ```

3. 将给出的运行代码保存为`exe.sh`,编写下列测试代码 

   ```bash
   #!/usr/bin/env bash
   
   number=0
   
   while true; do
       ((number++))
       result=$(bash ./exe.sh)
       if [ "$?" = "1" ]; then 
           echo $result
           echo "It has run for $number times."
           break
       fi
   done
   ```

   测试

   ```bash
   ricardo@g15:~/tmp$ bash test.sh
   The error was using magic numbers
   Something went wrong
   It has run for 228 times.
   ```

4. ```bash
   find . -name '*.html' | xargs tar
   ```









