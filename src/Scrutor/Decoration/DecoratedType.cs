using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Scrutor.Decoration
{
    internal class DecoratedType : Type
    {
        private readonly Type _proxiedType;

        public DecoratedType(Type type) => _proxiedType = type;

        // We use object reference equality here to ensure that only the decorating object can match.

        public override bool Equals(Type? o) => ReferenceEquals(this, o);

        public override bool Equals(object? o) => ReferenceEquals(this, o);

        public override int GetHashCode() => _proxiedType.GetHashCode();

        public override string? Namespace => _proxiedType.Namespace;
        public override string? AssemblyQualifiedName => _proxiedType.AssemblyQualifiedName;
        public override string? FullName => _proxiedType.FullName;


        public override Assembly Assembly => _proxiedType.Assembly;
        public override Module Module => _proxiedType.Module;

        public override Type? DeclaringType => _proxiedType.DeclaringType;
        public override MethodBase? DeclaringMethod => _proxiedType.DeclaringMethod;

        public override Type? ReflectedType => _proxiedType.ReflectedType;
        public override Type UnderlyingSystemType => _proxiedType.UnderlyingSystemType;

#if NETCOREAPP3_1_OR_GREATER
        public override bool IsTypeDefinition => _proxiedType.IsTypeDefinition;
#endif
        protected override bool IsArrayImpl() => _proxiedType.HasElementType;
        protected override bool IsByRefImpl() => _proxiedType.IsByRef;
        protected override bool IsPointerImpl() => _proxiedType.IsPointer;

        public override bool IsConstructedGenericType => _proxiedType.IsConstructedGenericType;
        public override bool IsGenericParameter => _proxiedType.IsGenericParameter;
#if NETCOREAPP3_1_OR_GREATER
        public override bool IsGenericTypeParameter => _proxiedType.IsGenericTypeParameter;
        public override bool IsGenericMethodParameter => _proxiedType.IsGenericMethodParameter;
#endif
        public override bool IsGenericType => _proxiedType.IsGenericType;
        public override bool IsGenericTypeDefinition => _proxiedType.IsGenericTypeDefinition;

#if NETCOREAPP3_1_OR_GREATER
        public override bool IsSZArray => _proxiedType.IsSZArray;
        public override bool IsVariableBoundArray => _proxiedType.IsVariableBoundArray;

        public override bool IsByRefLike => _proxiedType.IsByRefLike;
#endif
        protected override bool HasElementTypeImpl() => _proxiedType.HasElementType;
        public override Type? GetElementType() => _proxiedType.GetElementType();

        public override int GetArrayRank() => _proxiedType.GetArrayRank();

        public override Type GetGenericTypeDefinition() => _proxiedType.GetGenericTypeDefinition();
        public override Type[] GetGenericArguments() => _proxiedType.GetGenericArguments();

        public override int GenericParameterPosition => _proxiedType.GenericParameterPosition;
        public override GenericParameterAttributes GenericParameterAttributes => _proxiedType.GenericParameterAttributes;
        public override Type[] GetGenericParameterConstraints() => _proxiedType.GetGenericParameterConstraints();

        protected override TypeAttributes GetAttributeFlagsImpl() => _proxiedType.Attributes;

        protected override bool IsCOMObjectImpl() => _proxiedType.IsCOMObject;
        protected override bool IsContextfulImpl() => _proxiedType.IsContextful;

        public override bool IsEnum => _proxiedType.IsEnum;
        protected override bool IsMarshalByRefImpl() => _proxiedType.IsMarshalByRef;
        protected override bool IsPrimitiveImpl() => _proxiedType.IsPrimitive;

        protected override bool IsValueTypeImpl() => _proxiedType.IsValueType;
#if NETCOREAPP3_1_OR_GREATER
        public override bool IsSignatureType =>_proxiedType.IsSignatureType;
#endif
        public override bool IsSecurityCritical => _proxiedType.IsSecurityCritical;
        public override bool IsSecuritySafeCritical => _proxiedType.IsSecuritySafeCritical;
        public override bool IsSecurityTransparent => _proxiedType.IsSecurityTransparent;

        public override StructLayoutAttribute? StructLayoutAttribute => _proxiedType.StructLayoutAttribute;

        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
            => _proxiedType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => _proxiedType.GetConstructors(bindingAttr);

        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => _proxiedType.GetEvent(name, bindingAttr);

        public override EventInfo[] GetEvents() => _proxiedType.GetEvents();

        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => _proxiedType.GetEvents(bindingAttr);

        public override FieldInfo? GetField(string name, BindingFlags bindingAttr) => _proxiedType.GetField(name, bindingAttr);

        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => _proxiedType.GetFields(bindingAttr);

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr) => _proxiedType.GetMember(name, bindingAttr);

        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) => _proxiedType.GetMember(name, type, bindingAttr);

#if NET6_0
        public override MemberInfo GetMemberWithSameMetadataDefinitionAs(MemberInfo member) => _proxiedType.GetMemberWithSameMetadataDefinitionAs(member);
#endif
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => _proxiedType.GetMembers(bindingAttr);

        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
           => _proxiedType.GetMethod(name, bindingAttr, binder, callConvention, types!, modifiers);

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => _proxiedType.GetMethods(bindingAttr);

        public override Type? GetNestedType(string name, BindingFlags bindingAttr) => _proxiedType.GetNestedType(name, bindingAttr);

        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => _proxiedType.GetNestedTypes(bindingAttr);

        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
            => _proxiedType.GetProperty(name, bindingAttr, binder, returnType, types!, modifiers);

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => _proxiedType.GetProperties(bindingAttr);

        public override MemberInfo[] GetDefaultMembers() => _proxiedType.GetDefaultMembers();

        public override RuntimeTypeHandle TypeHandle => _proxiedType.TypeHandle;

        protected override TypeCode GetTypeCodeImpl() => Type.GetTypeCode(_proxiedType);

        public override Guid GUID => _proxiedType.GUID;

        public override Type? BaseType => _proxiedType.BaseType;

        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) =>
            _proxiedType.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);

        public override Type? GetInterface(string name, bool ignoreCase) => _proxiedType.GetInterface(name, ignoreCase);
        public override Type[] GetInterfaces() => _proxiedType.GetInterfaces();

        public override InterfaceMapping GetInterfaceMap(Type interfaceType) => _proxiedType.GetInterfaceMap(interfaceType);

        public override bool IsInstanceOfType(object? o) => _proxiedType.IsInstanceOfType(o);

        public override bool IsEquivalentTo(Type? other) => _proxiedType.IsEquivalentTo(other);

        public override Type GetEnumUnderlyingType() => _proxiedType.GetEnumUnderlyingType();

        public override Array GetEnumValues() => _proxiedType.GetEnumValues();

        public override Type MakeArrayType() => _proxiedType.MakeArrayType();
        public override Type MakeArrayType(int rank) => _proxiedType.MakeArrayType(rank);
        public override Type MakeByRefType() => _proxiedType.MakeByRefType();

        public override Type MakeGenericType(params Type[] typeArguments) => _proxiedType.MakeGenericType(typeArguments);

        public override Type MakePointerType() => _proxiedType.MakePointerType();

        public override string ToString() => "Type: " + Name;

        #region MemberInfo overrides

        public override MemberTypes MemberType => _proxiedType.MemberType;

        public override string Name => "Decorated " + _proxiedType.Name;

        public override IEnumerable<CustomAttributeData> CustomAttributes => _proxiedType.CustomAttributes;

        public override int MetadataToken => _proxiedType.MetadataToken;

        public override object[] GetCustomAttributes(bool inherit) => _proxiedType.GetCustomAttributes(inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _proxiedType.GetCustomAttributes(attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit) => _proxiedType.IsDefined(attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() => _proxiedType.GetCustomAttributesData();

        #endregion
    }
}
