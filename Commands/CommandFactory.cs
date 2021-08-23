using System;
using System.Reflection;


namespace TelegramBotService.Commands
{
  public static class CommandFactory
  {
    public static ICommand FromText(string request)
    {
      if (string.IsNullOrEmpty(request))
        return null;

      var args = request.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

      if (args.Length == 0)
        return null;

      var commandStr = args[0].TrimStart('/').ToLowerInvariant();
      var commandType = typeof(ICommand);

      foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
      {
        if (!type.IsInterface && !type.IsAbstract && (type.GetInterface(commandType.FullName) != null) && type.Name.ToLowerInvariant().Equals(commandStr))
          return (ICommand)Activator.CreateInstance(type);
      }

      return null;
    }
  }
}
