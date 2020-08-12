If you get this type of error:

      Unable to launch the IIS Express Web server.

      Failed to register URL "http://localhost:63591/" for site "xxxxxx" application
      "/". Error description: The process cannot access the file because it is being
      used by another process. (0x80070020)

Try this https://stackoverflow.com/a/59610178/403671

Check the port is not in the excluded port range which you can see by running the command:

      netsh interface ipv4 show excludedportrange protocol=tcp
