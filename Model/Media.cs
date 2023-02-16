using TelegramBotService.Model.Enums;


namespace TelegramBotService.Model
{
    public class Media : Message
    {
        public string? FileName { get; set; }

        public MediaType Type { get; set; }

        public byte[] Data { get; set; }


        public Media() : base()
        {
            Data = Array.Empty<byte>();
        }


        public Media(MediaType type, byte[] data, string? caption = null) : base(caption ?? string.Empty)
        {
            Type = type;
            Data = data;
        }


        public Media(string fileName, MediaType type, byte[] data, string? caption = null) : base(caption ?? string.Empty)
        {
            FileName = fileName;
            Type = type;
            Data = data;
        }


        public override string ToString() => $"{Text} - [{Type}]";
    }
}
