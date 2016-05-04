using Microsoft.Bot.Connector;

namespace OCRBot
{
    public class UserData
    {
        public UserData(ChannelAccount _user)
        {
            user = _user;

            // Database access will be here
            _totalPages = 0;
            _remainingPages = _totalPages;
            MasterMode = false;
        }

        private ChannelAccount user;

        private int _totalPages;
        private int _remainingPages;

        public bool MasterMode { set; get; }

        public int RemainingPages
        {
            get
            {
                return _remainingPages;
            }
        }

        public int TotalPages
        {
            get
            {
                return _totalPages;
            }
        }

        public void AddPages( int count )
        {
            _remainingPages += count;
            _totalPages += count;
        }

        public bool TrySubtractPages( int count )
        {
            if( _remainingPages >= count )
            {
                _remainingPages -= count;
                return true;
            }
            return false;
        }

        public string UserInfoString()
        {
            string info = $"**{user.Name}**, you have *{RemainingPages}* remaining pages out of total *{TotalPages}*.";
            if( MasterMode )
            {
                info += $"/r/n/r/nYou are in master mode.";
            }
            return info;
        }
    }
}