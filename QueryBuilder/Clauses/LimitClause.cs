namespace SqlKata
{
    public class LimitClause : AbstractClause
    {
        private long _limit;
        
        public int Limit
        {
            get => System.Convert.ToInt32(_limit);
            set => _limit = value > 0 ? value : _limit;
        }
        public long LongLimit
        {
            get => _limit;
            set => _limit = value > 0 ? value : _limit;
        }

        public bool HasLimit()
        {
            return _limit > 0;
        }

        public LimitClause Clear()
        {
            _limit = 0;
            return this;
        }

        /// <inheritdoc />
        public override AbstractClause Clone()
        {
            return new LimitClause
            {
                Engine = Engine,
                Limit = Limit,
                Component = Component,
            };
        }
    }
}