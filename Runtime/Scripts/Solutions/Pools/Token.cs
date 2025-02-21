using System;

namespace Metamatic
{
    public readonly struct Token64Bit
    {
        public readonly int ID;
        public readonly int Signature;
        
        public Token64Bit(int id, int signature)
        {
            ID = id;
            Signature = signature;
        }
        
        /* Get hash code and compare methods */
        public override int GetHashCode()
        {
            return ID.GetHashCode() ^ Signature.GetHashCode();
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Token64Bit other)
            {
                return ID == other.ID && Signature == other.Signature;
            }
            return false;
        }
    }
    
    public readonly struct Token96Bit
    {
        public static Token96Bit Empty => new Token96Bit(-1, -1);
        public readonly int ID;
        public readonly long Signature;
        
        public Token96Bit(int id, long signature)
        {
            ID = id;
            Signature = signature;
        }
        
        /* Get hash code and compare methods */
        public override int GetHashCode()
        {
            return ID.GetHashCode() ^ Signature.GetHashCode();
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Token96Bit other)
            {
                return ID == other.ID && Signature == other.Signature;
            }
            return false;
        }
    }
}