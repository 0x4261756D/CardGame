using System.Text;
using System.Collections.Generic;
using System;

namespace CardGameUtils;

internal class WeirdSVGInterpreter
{
	public static (byte, byte, byte) IntepretNameAsRGB(string name)
	{
		byte r = 0, g = 0, b = 0;
		for(int i = 0; i < name.Length - 1; i++)
		{
			r += (byte)((((byte)name[i]) & 0b1111) ^ (byte)(((byte)name[i + 1]) & 0b1111));
			g += (byte)(((((byte)name[i]) >> 4) & 0b1111) ^ ((((byte)name[i]) >> 4) & 0b1111));
			b += (byte)((byte)name[i] * (byte)name[i + 1]);
		}
		return (r, g, b);
	}
	public static string InterpretNameAsWeirdImage(string name)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(name);
		for(int i = 0; i < bytes.Length; i++)
		{
			byte lower = (byte)(((bytes[i] & (1 << 0)) >> 0) +
			((bytes[i] & (1 << 2)) >> 1) +
			((bytes[i] & (1 << 4)) >> 2) +
			((bytes[i] & (1 << 6)) >> 3));
			lower ^= (byte)i;
			byte upper = (byte)(((bytes[i] & (1 << 1)) << 3) +
			((bytes[i] & (1 << 3)) << 2) +
			((bytes[i] & (1 << 5)) << 1) +
			((bytes[i] & (1 << 7)) << 0));
			upper ^= (byte)(1 - i);
			bytes[i] = (byte)(lower + upper);
		}
		Queue<int> queue = new();
		StringBuilder svgPathInfo = new();
		for(int bit = 0; bit < bytes.Length * 8; bit += 5)
		{
			int i = bit / 8;
			int pos = bit % 8;
			if(i > bytes.Length - 2)
			{
				break;
			}
			int twoBytes = ((bytes[i + 1] << 8) + bytes[i]);
			byte instruction = (byte)((twoBytes >> pos) & 0b11111);
			switch(instruction)
			{
				// Add
				case 0:
				{
					int a = pos;
					if(queue.Count > 0)
					{
						a = queue.Dequeue();
					}
					int b = i;
					if(queue.Count > 0)
					{
						b = queue.Dequeue();
					}
					queue.Enqueue(a + b);
				}
				break;
				// Push
				case 1:
				{
					queue.Enqueue(twoBytes);
				}
				break;
				// Pop
				case 2:
				{
					if(queue.Count > 0)
					{
						_ = queue.Dequeue();
					}
				}
				break;
				// Move to
				case 3:
				{
					InterpretMoveTo(queue, svgPathInfo, i, pos, absolute: true);
				}
				break;
				// XOR
				case 4:
				{
					int a = 42;
					if(queue.Count > 0)
					{
						a = queue.Dequeue();
					}
					int b = 0;
					if(queue.Count > 0)
					{
						b = queue.Dequeue();
					}
					queue.Enqueue(a ^ b);
				}
				break;
				// Line
				case 5:
				{
					InterpretLine(queue, svgPathInfo, i, pos, absolute: true);
				}
				break;
				// Duplicate
				case 6:
				{
					int a = -42;
					if(queue.Count > 0)
					{
						a = queue.Dequeue();
					}
					queue.Enqueue(a);
					queue.Enqueue(a);
				}
				break;
				// Full bezier absolute
				case 7:
				{
					if(svgPathInfo.Length == 0)
					{
						_ = svgPathInfo.Append("M4 2");
					}
					bool first = true;
					while(queue.Count > 0)
					{
						if(first)
						{
							_ = svgPathInfo.Append('Q');
							first = false;
						}
						else
						{
							_ = svgPathInfo.Append(' ');
						}
						int x1Whole = queue.Dequeue();
						int x1Frac = pos;
						if(queue.Count > 0)
						{
							x1Frac = Math.Abs(queue.Dequeue());
						}
						int y1Whole = pos;
						if(queue.Count > 0)
						{
							y1Whole = queue.Dequeue();
						}
						int y1Frac = 42;
						if(queue.Count > 0)
						{
							y1Frac = Math.Abs(queue.Dequeue());
						}
						int xWhole = i;
						if(queue.Count > 0)
						{
							xWhole = queue.Dequeue();
						}
						int xFrac = 0;
						if(queue.Count > 0)
						{
							xFrac = queue.Dequeue();
						}
						int yWhole = 42;
						if(queue.Count > 0)
						{
							yWhole = queue.Dequeue();
						}
						int yFrac = 5;
						if(queue.Count > 0)
						{
							yFrac = queue.Dequeue();
						}
						_ = svgPathInfo.Append(x1Whole % 100).Append('.').Append(Math.Abs(x1Frac) % 10000).Append(' ')
						.Append(y1Whole % 100).Append('.').Append(Math.Abs(y1Frac) % 10000).Append(' ')
						.Append(xWhole % 100).Append('.').Append(Math.Abs(xFrac) % 10000).Append(' ')
						.Append(yWhole % 100).Append('.').Append(Math.Abs(yFrac) % 10000);
					}
				}
				break;
				case 8:
				{
					queue.Enqueue(bit);
				}
				break;
				case 9:
				{
					if(queue.Count > 0)
					{
						int n = bit + Math.Abs(queue.Dequeue()) + 4;
						if(n < bytes.Length * 8)
						{
							bit = n;
						}
						else
						{
							bit += 2;
						}
					}
				}
				break;
				case 10:
				{
					InterpretMoveTo(queue, svgPathInfo, i: i, pos: pos, absolute: true);
					InterpretLine(queue, svgPathInfo, i: i, pos: pos, absolute: true);
					InterpretMoveTo(queue, svgPathInfo, i: pos, pos: i, absolute: false);
					InterpretLine(queue, svgPathInfo, i: pos, pos: i, absolute: false);
				}
				break;
				case 11:
				{
					bit += 3;
				}
				break;
				case 12:
				{
					InterpretMoveTo(queue, svgPathInfo, i: i, pos: pos, absolute: false);
				}
				break;
				case 13:
				{
					InterpretLine(queue, svgPathInfo, i: i, pos: pos, absolute: false);
				}
				break;
				case 14:
				{
					int x = queue.Count;
					for(int j = 0; j < x; j++)
					{
						queue.Enqueue(x ^ queue.Dequeue());
					}
				}
				break;
				case 15:
				{
					bool absolute = false;
					if(queue.Count > 0)
					{
						absolute = queue.Dequeue() % 7 == 3;
					}
					InterpretLine(queue, svgPathInfo, i: i, pos: pos, absolute: absolute);
				}
				break;
				case 16:
				{
					if(svgPathInfo.Length == 0)
					{
						InterpretMoveTo(queue, svgPathInfo, i: pos, pos: i, absolute: true);
					}
					if(queue.Count == 0)
					{
						for(int j = 0; j < bytes.Length % 13; j++)
						{
							queue.Enqueue(bytes[j] + bytes[bytes.Length - j - 1]);
						}
						break;
					}
					int count = queue.Dequeue() % 23;
					bool absolute = false;
					if(queue.Count > 0)
					{
						absolute = queue.Dequeue() >= 42;
					}
					_ = svgPathInfo.Append(absolute ? 'T' : 't');
					for(int j = 0; j < Math.Abs(count) + 1; j++)
					{
						if(j > 0)
						{
							_ = svgPathInfo.Append(' ');
						}
						int y = count * j + pos;
						if(queue.Count > 0)
						{
							y = queue.Dequeue();
						}
						int x = ((i + 1) << j) + pos;
						if(queue.Count > 0)
						{
							x = queue.Dequeue();
						}
						_ = svgPathInfo.Append(x % 100).Append(' ').Append(y % 100);
					}
				}
				break;
				case 17:
				{
					int count = 17;
					if(queue.Count > 0)
					{
						count = queue.Dequeue();
					}
					for(int j = 0; j < count % 42; j++)
					{
						int x = (1 << j) + count - pos;
						if(queue.Count > 0)
						{
							x ^= queue.Dequeue();
						}
						queue.Enqueue(x);
						queue.Enqueue(x / 14);
						if(x < 42)
						{
							queue.Enqueue(j - pos);
							queue.Enqueue(42);
						}
					}
				}
				break;
				case 18:
				{
					InterpretArc(queue, svgPathInfo, i: i, pos: pos, absolute: true, count: 4);
					_ = svgPathInfo.Append('Z');
					InterpretMoveTo(queue, svgPathInfo, i: i, pos: pos, absolute: bit % 13 == 0);
				}
				break;
				case 19:
				{
					int yWhole = (bytes.Length + pos) ^ queue.Count;
					queue.Enqueue(yWhole);
					int yFrac = queue.Dequeue();
					queue.Enqueue(yFrac);
					bool absolute = queue.Dequeue() < 42;
					_ = svgPathInfo.Append(absolute ? 'V' : 'v').Append(yWhole % 100).Append('.').Append(Math.Abs(yFrac) % 10000);
				}
				break;
				case 20:
				{
					if(svgPathInfo.Length == 0)
					{
						InterpretMoveTo(queue, svgPathInfo, i: i, pos: pos, absolute: queue.Count % 2 == 0);
					}
					if(queue.Count == 0)
					{
						queue.Enqueue(20);
						queue.Enqueue(twoBytes);
						queue.Enqueue(~twoBytes);
						break;
					}
					_ = svgPathInfo.Append((twoBytes % 13 < 6) ? 'A' : 'a');
					bool first = true;
					while(queue.Count > 0)
					{
						if(first)
						{
							first = false;
						}
						else
						{
							_ = svgPathInfo.Append(' ');
						}
						int rxWhole = queue.Dequeue();
						rxWhole += Math.Sign(rxWhole);
						if(rxWhole % 100 == 0)
						{
							rxWhole = 10;
						}
						queue.Enqueue(rxWhole ^ twoBytes);
						int rxFrac = queue.Dequeue();
						int ryWhole = i + svgPathInfo.Length;
						if(queue.Count > 0)
						{
							ryWhole = queue.Dequeue();
						}
						ryWhole += Math.Sign(ryWhole);
						if(ryWhole % 100 == 0)
						{
							ryWhole = 10;
						}
						queue.Enqueue(pos - ryWhole);
						queue.Enqueue(twoBytes);
						queue.Enqueue(svgPathInfo.Length * twoBytes);
						int ryFrac = queue.Dequeue() + pos;
						int xAxisRotation = queue.Peek();
						queue.Enqueue(ryFrac ^ xAxisRotation);
						int largeArcFlag = queue.Dequeue();
						int sweepFlag = queue.Dequeue();
						queue.Enqueue(largeArcFlag + sweepFlag);
						int xWhole = queue.Dequeue();
						int xFrac = queue.Dequeue();
						queue.Enqueue(xWhole - xFrac);
						queue.Enqueue(ryFrac - xWhole);
						int yWhole = queue.Dequeue();
						int yFrac = queue.Dequeue();
						queue.Enqueue(xWhole * yFrac + xFrac * yWhole);
						_ = svgPathInfo.Append(rxWhole % 100).Append('.').Append(Math.Abs(rxFrac) % 10000).Append(' ')
						.Append(ryWhole % 100).Append('.').Append(Math.Abs(ryFrac) % 10000).Append(' ')
						.Append(xAxisRotation % 360).Append(' ').Append(Math.Abs(largeArcFlag % 2)).Append(' ')
						.Append(Math.Abs(sweepFlag % 2)).Append(' ')
						.Append(xWhole % 100).Append('.').Append(Math.Abs(xFrac) % 10000).Append(' ')
						.Append(yWhole % 100).Append('.').Append(Math.Abs(yFrac) % 10000);
						// At this point the queue has at least one element
						queue.Enqueue(queue.Dequeue() ^ twoBytes);
						int a = queue.Dequeue();
						if(queue.Count > 0)
						{
							int b = queue.Dequeue();
							int c = 4;
							if(queue.Count > 0)
							{
								c = queue.Dequeue();
							}
							queue.Enqueue(a ^ b - c);
						}
					}
				}
				break;
				case 21:
				{
					int offset = 3;
					if(queue.Count > 0)
					{
						offset = queue.Dequeue();
					}
					bit += ((offset ^ twoBytes) % 13) + 1;
				}
				break;
				case 22:
				{
					if(svgPathInfo.Length == 0)
					{
						InterpretMoveTo(queue, svgPathInfo, i: i, pos: pos, absolute: false);
						InterpretLine(queue, svgPathInfo, i: i, pos: pos, absolute: true);
					}
					queue.Enqueue(svgPathInfo.Length);
					queue.Enqueue(twoBytes ^ i);
					InterpretArc(queue, svgPathInfo, i: i, pos: pos, absolute: queue.Dequeue() % 2 == 0, queue.Dequeue() % 15);
				}
				break;
				case 23:
				{
					InterpretLine(queue, svgPathInfo, i: i, pos: pos, absolute: false);
					InterpretArc(queue, svgPathInfo, i: pos, pos: i, absolute: false, count: 3);
				}
				break;
				case 24:
				{
					bit -= 1;
				}
				break;
				case 25:
				{
					InterpretArc(queue, svgPathInfo, i: i, pos: pos, absolute: true, count: 4);
					InterpretArc(queue, svgPathInfo, i: pos, pos: i, absolute: false, count: 4);
					queue.Enqueue(25);
					queue.Enqueue(twoBytes);
					queue.Enqueue(bytes[(i ^ twoBytes) % bytes.Length]);
				}
				break;
				case 26:
				{
					InterpretLine(queue, svgPathInfo, i: i, pos: pos, absolute: false);
					InterpretLine(queue, svgPathInfo, i: pos, pos: i, absolute: false);
					InterpretLine(queue, svgPathInfo, i: -i, pos: -pos, absolute: false);
					InterpretLine(queue, svgPathInfo, i: -pos, pos: -i, absolute: false);
				}
				break;
				case 27:
				{
					int count = 5;
					if(queue.Count > 0)
					{
						count = queue.Dequeue() % 15;
					}
					InterpretCubic(queue, svgPathInfo, i: i, pos: pos, absolute: false, count: count);
				}
				break;
				case 28:
				{
					InterpretArc(queue, svgPathInfo, i: i, pos: pos, absolute: true, count: 2);
					InterpretCubic(queue, svgPathInfo, i: i, pos: pos, absolute: false, count: 12);
					_ = svgPathInfo.Append('Z');
				}
				break;
				case 29:
				{
					queue.Enqueue(twoBytes);
					queue.Enqueue(queue.Count ^ svgPathInfo.Length);
					queue.Enqueue(queue.Dequeue() - queue.Dequeue());
				}
				break;
				case 30:
				{
					InterpretMoveTo(queue, svgPathInfo, i: 32, pos: 42, absolute: true);
					InterpretLine(queue, svgPathInfo, i: i, pos: pos, absolute: false);
					InterpretCubic(queue, svgPathInfo, i: i, pos: pos, absolute: false, count: 3);
					_ = svgPathInfo.Append('Z');
					queue.Enqueue(pos);
					queue.Enqueue(i);
					InterpretCubic(queue, svgPathInfo, i: i, pos: pos, absolute: queue.Dequeue() <= i, count: queue.Dequeue() % 20);
					InterpretLine(queue, svgPathInfo, i: i, pos: pos, absolute: false);
					InterpretMoveTo(queue, svgPathInfo, i: i, pos: pos, absolute: false);
					InterpretArc(queue, svgPathInfo, i: i, pos: pos, absolute: false, count: pos);
					InterpretLine(queue, svgPathInfo, i: i, pos: pos, absolute: false);
					InterpretCubic(queue, svgPathInfo, i: i, pos: pos, absolute: false, count: 5);
					_ = svgPathInfo.Append('Z');
				}
				break;
				case 31:
				{
					for(int j = 0; j < i; j++)
					{
						if(queue.Count > 0)
						{
							_ = queue.Dequeue();
						}
					}
					queue.Enqueue(31);
				}
				break;
			}
		}
		while(queue.Count > 0)
		{
			InterpretLine(queue, svgPathInfo, i: 4, pos: 6, absolute: true);
			InterpretCubic(queue, svgPathInfo, i: queue.Count, pos: 24, absolute: true, count: 5);
			for(int i = 0; i < 20; i++)
			{
				if(queue.Count > 0)
				{
					_ = queue.Dequeue();
				}
			}
		}
		// Console.WriteLine(svgPathInfo.ToString());
		return svgPathInfo.ToString();
	}
	private static void InterpretCubic(Queue<int> queue, StringBuilder builder, int i, int pos, bool absolute, int count)
	{
		if(builder.Length == 0)
		{
			InterpretMoveTo(queue, builder, i: 32, pos: 64, absolute: true);
		}
		_ = builder.Append(absolute ? 'C' : 'c');
		for(int j = 0; j < Math.Abs(count) + 1; j++)
		{
			if(j > 0)
			{
				_ = builder.Append(' ');
			}
			int x1 = j * count;
			if(queue.Count > 0)
			{
				x1 ^= queue.Dequeue();
			}
			queue.Enqueue(x1);
			int y1 = builder.Length + queue.Dequeue();
			queue.Enqueue(y1);
			queue.Enqueue(x1 ^ y1);
			int x2 = queue.Peek();
			queue.Enqueue(x2 * x1);
			int y2 = queue.Dequeue() + queue.Dequeue();
			queue.Enqueue(32);
			queue.Enqueue(y2);
			queue.Enqueue(x1 - x2 + y1 - y2);
			int x = queue.Dequeue();
			int y = queue.Dequeue();
			_ = builder.Append(x1 % 100).Append(' ').Append(y1 % 100).Append(' ')
			.Append(x2 % 100).Append(' ').Append(y2 % 100).Append(' ')
			.Append(x % 100).Append(' ').Append(y % 100);
		}
	}
	private static void InterpretArc(Queue<int> queue, StringBuilder builder, int i, int pos, bool absolute, int count)
	{
		if(builder.Length == 0)
		{
			InterpretMoveTo(queue, builder, i: pos, pos: i, absolute: !absolute);
			InterpretLine(queue, builder, i: i, pos: pos, absolute: absolute);
			InterpretLine(queue, builder, i: queue.Count, pos: count, absolute: false);
			InterpretMoveTo(queue, builder, i: count, pos: pos, absolute: absolute);
		}
		_ = builder.Append(absolute ? 'A' : 'a');
		for(int j = 0; j < Math.Abs(count) + 1; j++)
		{
			if(j > 0)
			{
				_ = builder.Append(' ');
				queue.Enqueue(11);
			}
			queue.Enqueue(j * pos + i);
			int rxWhole = queue.Dequeue();
			rxWhole += Math.Sign(rxWhole);
			if(rxWhole % 100 == 0)
			{
				rxWhole = 3;
			}
			queue.Enqueue(rxWhole ^ count);
			queue.Enqueue(42);
			int ryWhole = queue.Dequeue();
			ryWhole += Math.Sign(rxWhole);
			if(ryWhole % 100 == 0)
			{
				ryWhole = 3;
			}
			queue.Enqueue(rxWhole + ryWhole);
			queue.Enqueue(-12);
			int ryFrac = queue.Dequeue();
			queue.Enqueue(queue.Peek() ^ (ryFrac + rxWhole));
			int rxFrac = queue.Dequeue() + count;
			int xAxisRotation = j - count + queue.Dequeue();
			queue.Enqueue(xAxisRotation);
			queue.Enqueue(rxFrac - ryFrac);
			int largeArcFlag = queue.Dequeue();
			int sweepFlag = queue.Dequeue();
			queue.Enqueue(largeArcFlag ^ sweepFlag);
			queue.Enqueue(sweepFlag + xAxisRotation);
			queue.Enqueue(largeArcFlag - rxWhole);
			int xWhole = queue.Dequeue();
			int yWhole = queue.Dequeue();
			queue.Enqueue(rxFrac * yWhole);
			queue.Enqueue(ryWhole - xWhole);
			int xFrac = queue.Dequeue();
			int yFrac = queue.Dequeue();
			for(int k = 0; k < queue.Dequeue() % 5; k++)
			{
				queue.Enqueue(xFrac + i * (k + 1) - queue.Count);
			}
			_ = builder.Append(rxWhole % 100).Append('.').Append(Math.Abs(rxFrac) % 10000).Append(' ')
			.Append(ryWhole % 100).Append('.').Append(Math.Abs(ryFrac) % 10000).Append(' ')
			.Append(xAxisRotation % 360).Append(' ').Append(Math.Abs(largeArcFlag % 2)).Append(' ').Append(Math.Abs(sweepFlag % 2)).Append(' ')
			.Append(xWhole % 100).Append('.').Append(Math.Abs(xFrac) % 10000).Append(' ')
			.Append(yWhole % 100).Append('.').Append(Math.Abs(yFrac) % 10000);
		}
	}
	private static void InterpretMoveTo(Queue<int> queue, StringBuilder builder, int i, int pos, bool absolute)
	{
		int yWhole = i;
		if(queue.Count > 0)
		{
			yWhole = queue.Dequeue();
		}
		int xWhole = pos;
		if(queue.Count > 0)
		{
			xWhole = queue.Dequeue();
		}
		int xFrac = 0;
		if(queue.Count > 0)
		{
			xFrac = Math.Abs(queue.Dequeue());
		}
		int yFrac = pos ^ i;
		if(queue.Count > 0)
		{
			yFrac = Math.Abs(queue.Dequeue());
		}
		_ = builder.Append(absolute ? 'M' : 'm').Append(xWhole % 100).Append('.').Append(Math.Abs(xFrac) % 10000).Append(' ').Append(yWhole % 100).Append('.').Append(Math.Abs(yFrac) % 10000);
	}
	private static void InterpretLine(Queue<int> queue, StringBuilder builder, int i, int pos, bool absolute)
	{
		if(builder.Length == 0)
		{
			InterpretMoveTo(queue, builder, i: i, pos: pos, absolute: true);
		}
		int xWhole = i;
		if(queue.Count > 0)
		{
			xWhole = queue.Dequeue();
		}
		int yWhole = pos;
		if(queue.Count > 0)
		{
			yWhole = queue.Dequeue();
		}
		int xFrac = 0;
		if(queue.Count > 0)
		{
			xFrac = Math.Abs(queue.Dequeue());
		}
		int yFrac = pos ^ i;
		if(queue.Count > 0)
		{
			yFrac = Math.Abs(queue.Dequeue());
		}
		_ = builder.Append(absolute ? 'L' : 'l').Append(xWhole % 100).Append('.').Append(Math.Abs(xFrac) % 10000).Append(' ').Append(yWhole % 100).Append('.').Append(Math.Abs(yFrac) % 10000);
	}
}
