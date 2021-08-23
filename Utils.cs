using System;
using System.Net;


namespace TelegramBotService
{
  static class Utils
  {
    public static bool CanConnectToTelegram()
    {
      try
      {
        using (var client = new WebClientWithShortTimeout())
        using (client.OpenRead("https://api.telegram.org"))
          return true;
      }

      catch
      {
        return false;
      }
    }
  }


  class WebClientWithShortTimeout : WebClient
  {
    protected override WebRequest GetWebRequest(Uri uri)
    {
      var webRequest = base.GetWebRequest(uri);
      webRequest.Timeout = 3000;
      return webRequest;
    }
  }
}
