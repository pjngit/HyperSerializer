using System.Reflection;
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using Hyper;

namespace HyperSerializer.Dynamic.Syntax;

internal ref struct MemberTypeInfos<T>
{
    public Span<MemberTypeInfo> Members;
    public int Length;
    private const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField | BindingFlags.SetProperty;

    public ref MemberTypeInfo this[int index] => ref this.Members[index];

	public MemberTypeInfos()
	{
		var type = typeof(T);
		var fields = type.GetFields(bindingFlags);
		PropertyInfo[] properties = null;

		if (HyperSerializerSettings.SerializeFields)
		{
			properties = type.GetProperties(bindingFlags);
			this.Length = fields.Length + properties.Length;
		}
		else
		{
			this.Length = fields.Length;
		}

		this.Members = new MemberTypeInfo[this.Length];
		int memberIndex = 0;

		for (int i = 0; i < fields.Length; i++)
		{
			var field = fields[i];
			this.Members[memberIndex] = new MemberTypeInfo
			{
				PropertyType = field.FieldType,
				Name = field.Name,
				Ignore = field.IsDefined(typeof(IgnoreDataMemberAttribute))
			};
			memberIndex++;
		}

		if (properties != null)
		{
			for (int i = 0; i < properties.Length; i++)
			{
				var property = properties[i];
				this.Members[memberIndex] = new MemberTypeInfo
				{
					PropertyType = property.PropertyType,
					Name = property.Name,
					Ignore = property.IsDefined(typeof(IgnoreDataMemberAttribute))
				};
				memberIndex++;
			}
		}
	}
}

internal class MemberTypeInfo
{
    public Type PropertyType;
    public bool Ignore;
    public string Name;
}