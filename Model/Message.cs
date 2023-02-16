namespace TelegramBotService.Model
{
    public class Message
    {
        public string Text { get; set; }


        public Message()
        {
            Text = string.Empty;
        }


        public Message(string text)
        {
            Text = text;
        }


        public override string ToString() => Text;
    }
}
