using System;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using TelegramBotService.Model;


namespace TelegramBotService
{
  public class BotContext : DbContext
  {
    public DbSet<Setting> Settings { get; set; }


    public BotContext(DbContextOptions<BotContext> options)
      : base(options)
    {
      Database.ExecuteSqlRaw("pragma journal_mode = WAL;pragma synchronous = normal;pragma temp_store = memory");
    }


    public void StoreSetting(string name, object value)
    {
      var setting = Settings.SingleOrDefault(s => s.Name == name);
      string valueStr = null;

      if (value is byte[] ba)
        valueStr = Convert.ToBase64String(ba);
      else
        valueStr = value.ToString();

      if (setting == null)
        Settings.Add(new Setting { Name = name, Value = valueStr });
      else
        setting.Value = valueStr;
    }


    public T ReadSetting<T>(string name, T defaultValue = default)
    {
      T value = defaultValue;

      var setting = Settings.SingleOrDefault(s => s.Name == name);

      if (setting != null)
      {
        switch (value)
        {
          case bool b:

            b = Convert.ToBoolean(setting.Value);
            break;

          case long l:

            l = Convert.ToInt64(setting.Value);
            break;

          case string s:

            s = setting.Value;
            break;

          case byte[] ba:

            ba = Convert.FromBase64String(setting.Value);
            break;
        }
      }

      return value;
    }
  }
}
