namespace Shared
{
	using System;

	internal class Version : IComparable<Version>, IEquatable<Version>
	{
		public Version(int major, int minor, int patch)
		{
			Major = major;
			Minor = minor;
			Patch = patch;
		}

		public int Major { get; }

		public int Minor { get; }

		public int Patch { get; }

		public static bool operator ==(Version left, Version right)
		{
			if (ReferenceEquals(left, right))
			{
				return true;
			}

			if (left is null || right is null)
			{
				return false;
			}

			return left.Equals(right);
		}

		public static bool operator !=(Version left, Version right)
		{
			return !(left == right);
		}

		public static bool operator <(Version left, Version right)
		{
			if (left is null)
			{
				return right != null;
			}

			return left.CompareTo(right) < 0;
		}

		public static bool operator <=(Version left, Version right)
		{
			if (left is null)
			{
				return true;
			}

			return left.CompareTo(right) <= 0;
		}

		public static bool operator >(Version left, Version right)
		{
			if (left is null)
			{
				return false;
			}

			return left.CompareTo(right) > 0;
		}

		public static bool operator >=(Version left, Version right)
		{
			if (left is null)
			{
				return right is null;
			}

			return left.CompareTo(right) >= 0;
		}

		public static Version FromString(string version)
		{
			var split = version.Split('.');
			if (split.Length != 3)
			{
				throw new ArgumentException("Version string must be in the format 'Major.Minor.Patch'");
			}

			return new Version(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
		}

		public int CompareTo(Version other)
		{
			if (other == null)
			{
				return 1;
			}

			if (Major != other.Major)
			{
				return Major.CompareTo(other.Major);
			}

			if (Minor != other.Minor)
			{
				return Minor.CompareTo(other.Minor);
			}

			return Patch.CompareTo(other.Patch);
		}

		public bool Equals(Version other)
		{
			if (other == null)
			{
				return false;
			}

			return Major == other.Major &&
				Minor == other.Minor &&
				Patch == other.Patch;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as Version);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Major.GetHashCode();
				hash = (hash * 23) + Minor.GetHashCode();
				hash = (hash * 23) + Patch.GetHashCode();
				return hash;
			}
		}

		public override string ToString()
		{
			return $"{Major}.{Minor}.{Patch}";
		}
	}
}