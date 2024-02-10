using System;

namespace Rdr.Gui
{
	public class SeeRecentAmount
	{
		public int Amount { get; set; } = 0;

		public SeeRecentAmount()
			: this(2)
		{ }

		public SeeRecentAmount(int amount)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(amount);

			Amount = amount;
		}
	}
}
