using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Runtime.InteropServices;

namespace VsPsReplaceAccents
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class ReplaceAccentsOptions : BaseOptionPage<ReplaceAccentsSettings> { }
    }

    public class ReplaceAccentsSettings : BaseOptionModel<ReplaceAccentsSettings>
    {
        [Category("Replace Accents")]
        [DisplayName("Special Character Mappings")]
        [Description("Custom mappings for special characters (e.g., 'ß' to 'ss', 'æ' to 'ae')")]
        [Editor(typeof(CharMappingCollectionEditor), typeof(UITypeEditor))]
        public CharMappingCollection SpecialCharacterMappings { get; set; } = new CharMappingCollection();

        [Category("Replace Accents")]
        [DisplayName("Use Default Mappings")]
        [Description("Use default character mappings in addition to custom mappings")]
        [DefaultValue(true)]
        public bool UseDefaultMappings { get; set; } = true;
    }

    [Serializable]
    public class CharMappingCollection : List<CharMapping>, ICustomTypeDescriptor
    {
        // This enables the collection to be displayed in the property grid
        #region ICustomTypeDescriptor Implementation

        public AttributeCollection GetAttributes() => AttributeCollection.Empty;

        public string GetClassName() => typeof(CharMappingCollection).Name;

        public string GetComponentName() => null;

        public TypeConverter GetConverter() => null;

        public EventDescriptor GetDefaultEvent() => null;

        public PropertyDescriptor GetDefaultProperty() => null;

        public object GetEditor(Type editorBaseType) => null;

        public EventDescriptorCollection GetEvents() => EventDescriptorCollection.Empty;

        public EventDescriptorCollection GetEvents(Attribute[] attributes) => EventDescriptorCollection.Empty;

        public PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(Array.Empty<Attribute>());
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptor[] properties = new PropertyDescriptor[Count];

            for (int i = 0; i < Count; i++)
            {
                properties[i] = new CharMappingPropertyDescriptor(this, i);
            }

            return new PropertyDescriptorCollection(properties);
        }

        public object GetPropertyOwner(PropertyDescriptor pd) => this;

        #endregion

        // Helper method to convert to Dictionary
        public Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (CharMapping mapping in this)
            {
                if (
                    !string.IsNullOrEmpty(mapping.Character) &&
                    mapping.Character.Length == 1 &&
                    !string.IsNullOrEmpty(mapping.Replacement)
                )
                {
                    result[mapping.Character] = mapping.Replacement;
                }
            }

            return result;
        }
    }

    [Serializable]
    public class CharMapping
    {
        [DisplayName("Character")]
        [Description("The special character to be replaced")]
        public string Character { get; set; }

        [DisplayName("Replacement")]
        [Description("The replacement string")]
        public string Replacement { get; set; }

        public CharMapping() { }

        public CharMapping(string character, string replacement)
        {
            Character = character;

            Replacement = replacement;
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Character) && !string.IsNullOrWhiteSpace(Replacement))
            {
                return $"{Character} -> {Replacement}";
            }

            return string.Empty;
        }
    }

    // Custom property descriptor for the collection items
    internal class CharMappingPropertyDescriptor : PropertyDescriptor
    {
        private readonly CharMappingCollection _collection;

        private readonly int _index;

        public CharMappingPropertyDescriptor(CharMappingCollection collection, int index) : base(index.ToString(), null)
        {
            _collection = collection;
            _index = index;
        }

        public override Type ComponentType => typeof(CharMappingCollection);

        public override bool IsReadOnly => false;

        public override Type PropertyType => typeof(CharMapping);

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object component) => _collection[_index];

        public override void ResetValue(object component) { }

        public override void SetValue(object component, object value) => _collection[_index] = (CharMapping)value;

        public override bool ShouldSerializeValue(object component) => true;
    }

    // Custom editor for the collection that will appear in the property grid
    internal class CharMappingCollectionEditor : CollectionEditor
    {
        public CharMappingCollectionEditor(Type type) : base(type) { }

        protected override string GetDisplayText(object value)
        {
            if (value is CharMapping mapping)
            {
                return mapping.ToString();
            }

            return base.GetDisplayText(value);
        }

        protected override Type CreateCollectionItemType()
        {
            return typeof(CharMapping);
        }

        protected override object CreateInstance(Type itemType)
        {
            return new CharMapping();
        }
    }
}