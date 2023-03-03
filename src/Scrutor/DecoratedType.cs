using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Scrutor;

public class DecoratedType : Type
{
    public DecoratedType(Type type) => ProxiedType = type;
    private Type ProxiedType { get; }

    // We use object reference equality here to ensure that only the decorating object can match.
    public override bool Equals(Type? o) => ReferenceEquals(this, o);
    public override bool Equals(object? o) => ReferenceEquals(this, o);
    public override int GetHashCode() => ProxiedType.GetHashCode();
    public override string? Namespace => ProxiedType.Namespace;
    public override string? AssemblyQualifiedName => ProxiedType.AssemblyQualifiedName;
    public override string? FullName => ProxiedType.FullName;
    public override Assembly Assembly => ProxiedType.Assembly;
    public override Module Module => ProxiedType.Module;
    public override Type? DeclaringType => ProxiedType.DeclaringType;
    public override MethodBase? DeclaringMethod => ProxiedType.DeclaringMethod;
    public override Type? ReflectedType => ProxiedType.ReflectedType;
    public override Type UnderlyingSystemType => ProxiedType.UnderlyingSystemType;

#if NETCOREAPP3_1_OR_GREATER
        public override bool IsTypeDefinition => ProxiedType.IsTypeDefinition;
#endif
    protected override bool IsArrayImpl() => ProxiedType.HasElementType;
    protected override bool IsByRefImpl() => ProxiedType.IsByRef;
    protected override bool IsPointerImpl() => ProxiedType.IsPointer;
    public override bool IsConstructedGenericType => ProxiedType.IsConstructedGenericType;
    public override bool IsGenericParameter => ProxiedType.IsGenericParameter;
#if NETCOREAPP3_1_OR_GREATER
        public override bool IsGenericTypeParameter => ProxiedType.IsGenericTypeParameter;
        public override bool IsGenericMethodParameter => ProxiedType.IsGenericMethodParameter;
#endif
    public override bool IsGenericType => ProxiedType.IsGenericType;
    public override bool IsGenericTypeDefinition => ProxiedType.IsGenericTypeDefinition;
#if NETCOREAPP3_1_OR_GREATER
        public override bool IsSZArray => ProxiedType.IsSZArray;
        public override bool IsVariableBoundArray => ProxiedType.IsVariableBoundArray;
        public override bool IsByRefLike => ProxiedType.IsByRefLike;
#endif
    protected override bool HasElementTypeImpl() => ProxiedType.HasElementType;
    public override Type? GetElementType() => ProxiedType.GetElementType();
    public override int GetArrayRank() => ProxiedType.GetArrayRank();
    public override Type GetGenericTypeDefinition() => ProxiedType.GetGenericTypeDefinition();
    public override Type[] GetGenericArguments() => ProxiedType.GetGenericArguments();
    public override int GenericParameterPosition => ProxiedType.GenericParameterPosition;
    public override GenericParameterAttributes GenericParameterAttributes => ProxiedType.GenericParameterAttributes;
    public override Type[] GetGenericParameterConstraints() => ProxiedType.GetGenericParameterConstraints();
    protected override TypeAttributes GetAttributeFlagsImpl() => ProxiedType.Attributes;
    protected override bool IsCOMObjectImpl() => ProxiedType.IsCOMObject;
    protected override bool IsContextfulImpl() => ProxiedType.IsContextful;
    public override bool IsEnum => ProxiedType.IsEnum;
    protected override bool IsMarshalByRefImpl() => ProxiedType.IsMarshalByRef;
    protected override bool IsPrimitiveImpl() => ProxiedType.IsPrimitive;
    protected override bool IsValueTypeImpl() => ProxiedType.IsValueType;
#if NETCOREAPP3_1_OR_GREATER
    public override bool IsSignatureType => ProxiedType.IsSignatureType;
#endif
    public override bool IsSecurityCritical => ProxiedType.IsSecurityCritical;
    public override bool IsSecuritySafeCritical => ProxiedType.IsSecuritySafeCritical;
    public override bool IsSecurityTransparent => ProxiedType.IsSecurityTransparent;
    public override StructLayoutAttribute? StructLayoutAttribute => ProxiedType.StructLayoutAttribute;
    protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
        => ProxiedType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
    public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => ProxiedType.GetConstructors(bindingAttr);
    public override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => ProxiedType.GetEvent(name, bindingAttr);
    public override EventInfo[] GetEvents() => ProxiedType.GetEvents();
    public override EventInfo[] GetEvents(BindingFlags bindingAttr) => ProxiedType.GetEvents(bindingAttr);
    public override FieldInfo? GetField(string name, BindingFlags bindingAttr) => ProxiedType.GetField(name, bindingAttr);
    public override FieldInfo[] GetFields(BindingFlags bindingAttr) => ProxiedType.GetFields(bindingAttr);
    public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr) => ProxiedType.GetMember(name, bindingAttr);
    public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) => ProxiedType.GetMember(name, type, bindingAttr);
#if NET6_0
    public override MemberInfo GetMemberWithSameMetadataDefinitionAs(MemberInfo member) => ProxiedType.GetMemberWithSameMetadataDefinitionAs(member);
#endif
    public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => ProxiedType.GetMembers(bindingAttr);
    protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        => ProxiedType.GetMethod(name, bindingAttr, binder, callConvention, types!, modifiers);
    public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => ProxiedType.GetMethods(bindingAttr);
    public override Type? GetNestedType(string name, BindingFlags bindingAttr) => ProxiedType.GetNestedType(name, bindingAttr);
    public override Type[] GetNestedTypes(BindingFlags bindingAttr) => ProxiedType.GetNestedTypes(bindingAttr);
    protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        => ProxiedType.GetProperty(name, bindingAttr, binder, returnType, types!, modifiers);
    public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => ProxiedType.GetProperties(bindingAttr);
    public override MemberInfo[] GetDefaultMembers() => ProxiedType.GetDefaultMembers();
    public override RuntimeTypeHandle TypeHandle => ProxiedType.TypeHandle;
    protected override TypeCode GetTypeCodeImpl() => GetTypeCode(ProxiedType);
    public override Guid GUID => ProxiedType.GUID;
    public override Type? BaseType => ProxiedType.BaseType;
    public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) =>
        ProxiedType.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
    public override Type? GetInterface(string name, bool ignoreCase) => ProxiedType.GetInterface(name, ignoreCase);
    public override Type[] GetInterfaces() => ProxiedType.GetInterfaces();
    public override InterfaceMapping GetInterfaceMap(Type interfaceType) => ProxiedType.GetInterfaceMap(interfaceType);
    public override bool IsInstanceOfType(object? o) => ProxiedType.IsInstanceOfType(o);
    public override bool IsEquivalentTo(Type? other) => ProxiedType.IsEquivalentTo(other);
    public override Type GetEnumUnderlyingType() => ProxiedType.GetEnumUnderlyingType();
    public override Array GetEnumValues() => ProxiedType.GetEnumValues();
    public override Type MakeArrayType() => ProxiedType.MakeArrayType();
    public override Type MakeArrayType(int rank) => ProxiedType.MakeArrayType(rank);
    public override Type MakeByRefType() => ProxiedType.MakeByRefType();
    public override Type MakeGenericType(params Type[] typeArguments) => ProxiedType.MakeGenericType(typeArguments);
    public override Type MakePointerType() => ProxiedType.MakePointerType();
    public override string ToString() => "Type: " + Name;
    public override MemberTypes MemberType => ProxiedType.MemberType;
    public override string Name => $"{ProxiedType.Name}+Decorated";
    public override IEnumerable<CustomAttributeData> CustomAttributes => ProxiedType.CustomAttributes;
    public override int MetadataToken => ProxiedType.MetadataToken;
    public override object[] GetCustomAttributes(bool inherit) => ProxiedType.GetCustomAttributes(inherit);
    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => ProxiedType.GetCustomAttributes(attributeType, inherit);
    public override bool IsDefined(Type attributeType, bool inherit) => ProxiedType.IsDefined(attributeType, inherit);
    public override IList<CustomAttributeData> GetCustomAttributesData() => ProxiedType.GetCustomAttributesData();
}
