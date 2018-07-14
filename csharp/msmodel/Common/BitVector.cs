// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UR.Common
{
	/// <summary>
	/// Simple n-sized bit vector implementation
	/// </summary>
	public class BitVector
	{
		/// <summary>
		/// Initializes the bit vector with the specified number of bits
		/// </summary>
		/// <param name="bits">The number of bits to represent</param>
		public BitVector(int bits)
		{
			this.bits = (bits + 7) & ~0x7;

			Clear();
		}

		/// <summary>
		/// Sets a bit
		/// </summary>
		/// <param name="bit">The bit to set</param>
		public void Set(int bit)
		{
			if (bit >= bits)
                throw new ArgumentException(String.Format("The bit specified is invalid {0} >= {1}", bit, bits));

			bitv[bit / 8] |= (byte)(1 << (bit % 8));
		}

        public byte ReadByte(int bit)
        {
            byte value = 0;

            for (int pos = 0; pos < 8; pos++)
            {
                if (IsSet(bit + pos))
                {
                    value |= (byte)(1 << pos);
                }
            }

            return value;
        }

        public void SetByte(int bit, byte value)
        {
            for (int pos = 0; pos < 8; pos++)
            {
                if ((value & (1 << pos)) != 0)
                {
                    Set(bit + pos);
                }
                else
                {
                    Unset(bit + pos);
                }
            }
        }

		/// <summary>
		/// Clears a bit
		/// </summary>
		/// <param name="bit">The bit to clear</param>
		public void Unset(int bit)
		{
			if (bit >= bits)
				throw new ArgumentException(String.Format("The bit specified is invalid {0} >= {1}", bit, bits));

			bitv[bit / 8] &= (byte)(~(1 << (bit % 8)));
		}

        public bool IsSet(int bit)
        {
            if (bit >= bits)
                throw new ArgumentException(String.Format("The bit specified is invalid {0} >= {1}", bit, bits));

            if ((bitv[bit / 8] & (byte)((1 << (bit % 8)))) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

		/// <summary>
		/// Clears all bits
		/// </summary>
		public void Clear()
		{
			bitv = new byte[(bits / 8) + 1];
		}

		/// <summary>
		/// The number of bits represented
		/// </summary>
		private int bits;
		/// <summary>
		/// The array that contains the values of those bits
		/// </summary>
		private byte[] bitv;
	}

	/// <summary>
	/// Simple n-sized bit vector implementation
	/// </summary>
	public class GenericBitVector<T> : IEnumerable
	{
		/// <summary>
		/// Initializes the bit vector with the specified number of bits
		/// </summary>
		/// <param name="bits">The number of bits to represent</param>
		public GenericBitVector(int bits)
		{
			this.bits = bits;

			Clear();
		}

		/// <summary>
		/// Calls Get(index)
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public T this[int index]
		{
			get { return (T)Get(index); }
			set { Set(index, value); }
		}

		/// <summary>
		/// Returns the bit set enumerator
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetEnumerator()
		{
			return bitv.GetEnumerator();
		}

		/// <summary>
		/// Checks to see if a bit has been set
		/// </summary>
		/// <param name="bit">The bit to check</param>
		/// <returns></returns>
		public bool IsSet(int bit)
		{
			return (bitv[bit] != null);
		}

		/// <summary>
		/// Sets a bit
		/// </summary>
		/// <param name="bit">The bit to set</param>
		public void Set(int bit, object val)
		{
			CheckBit(bit);

			bitv[bit] = (T)val;
		}

		public object Get(int bit)
		{
			CheckBit(bit);

			return bitv[bit];
		}

		/// <summary>
		/// Clears a bit
		/// </summary>
		/// <param name="bit">The bit to clear</param>
		public void Unset(int bit)
		{
			CheckBit(bit);

			bitv[bit] = default(T);
		}

		private void CheckBit(int bit)
		{
			if (bit >= bits)
				throw new ArgumentException("The bit specified is invalid");
		}

		/// <summary>
		/// Clears all bits
		/// </summary>
		public void Clear()
		{
			bitv = new T[bits];
		}

		/// <summary>
		/// The number of bits represented
		/// </summary>
		private int bits;
		/// <summary>
		/// The array that contains the values of those bits
		/// </summary>
		private T[] bitv;
	}
}
