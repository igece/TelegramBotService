using System.ComponentModel.DataAnnotations;


namespace TelegramBotService.Model
{
  public class Setting
  {
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }

    public string Value { get; set; }
  }
}
