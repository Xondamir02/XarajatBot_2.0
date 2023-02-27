namespace XarajatBot2.Models
{
    class User
    {
        public long ChatId;
        public string Name;
        public ENextMessage NextMessage;

        public Outlay? CurrentAddingOutlay;
    }
}
