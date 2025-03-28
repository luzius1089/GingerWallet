using System.ComponentModel;
using System.Linq;
using WalletWasabi.Lang;
using WalletWasabi.Models;

namespace WalletWasabi.Extensions;

public static class EnumExtensions
{
	public static string? GetDescription<T>(this T? value) where T : Enum
	{
		if (value is null)
		{
			return null;
		}

		var fieldInfo = value.GetType().GetField(value.ToString());
		var attribArray = fieldInfo!.GetCustomAttributes(false);

		if (attribArray.Length == 0)
		{
			return value.ToString();
		}

		DescriptionAttribute? attrib = null;

		foreach (var att in attribArray)
		{
			if (att is DescriptionAttribute attribute)
			{
				attrib = attribute;
			}
		}

		return attrib == null ? value.ToString() : attrib.Description;
	}

	public static T? GetFirstAttribute<T>(this Enum value) where T : Attribute
	{
		var stringValue = value.ToString();
		var type = value.GetType();
		var memberInfo = type.GetMember(stringValue).FirstOrDefault()
			?? throw new InvalidOperationException($"Enum of type '{typeof(T).FullName}' does not contain value '{stringValue}'");

		var attributes = memberInfo.GetCustomAttributes(typeof(T), false);

		return attributes.Length != 0 ? (T)attributes[0] : null;
	}

	public static string FriendlyName(this Enum value)
	{
		var attribute = value.GetFirstAttribute<FriendlyNameAttribute>();

		if (attribute is null)
		{
			return value.ToString();
		}

		if (attribute.IsLocalized)
		{
			var localizedValue = Resources.ResourceManager.GetString($"{value.ToString()}Enum", Resources.Culture);
			if (!string.IsNullOrEmpty(localizedValue))
			{
				return localizedValue;
			}
		}

		if (!string.IsNullOrEmpty(attribute.FriendlyName))
		{
			return attribute.FriendlyName;
		}

		return value.ToString();
	}

	public static T? GetEnumValueOrDefault<T>(this int value, T defaultValue) where T : Enum
	{
		if (Enum.IsDefined(typeof(T), value))
		{
			return (T?)Enum.ToObject(typeof(T), value);
		}

		return defaultValue;
	}
}
