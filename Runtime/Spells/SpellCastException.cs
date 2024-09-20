using System;

namespace Metamatic
{
    public class SpellCastException: Exception
    {
        public readonly Type SpellType;
        public SpellCastException(ISyncSpell syncSpell, string message): base(message)
        {
            SpellType = syncSpell.GetType();
        }
    }
}