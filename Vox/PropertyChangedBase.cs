using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace Vox
{
    //PropertyChangedBase -> ObjectVerve -> ResourceVerve -> StructureVerve -> ElementVerve -> VOM

    //A class that holds the implementation details for INotifyPropertyChanged.
    public class PropertyChangedBase : INotifyPropertyChanged, ICloneable
    {
        #region Constructors
        //Default constructor
        public PropertyChangedBase()
        {
        }

        //Copy constructor
        public PropertyChangedBase(PropertyChangedBase Original, bool AsShallow) : this()
        {
            if (!AsShallow)
                CopyFrom(Original, AsShallow);
        }

        public void CopyFrom(PropertyChangedBase Original, bool AsShallow)
        {
            //Not sure I want to actually clone the event's hooks... things get weird.
            //PropertyChanged = Original.PropertyChanged;
        }
        #endregion

        #region Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void RaisePropertyChanged([CallerMemberName] string PropertyName = "")
        {
            RaisePropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
        }

        public virtual void RaisePropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (sender == null)
                sender = this;
            //if (args.PropertyName == "SourceInterfaceName" || args.PropertyName == "ActiveBatchElement")
            //    Log.Debug("Property " + args.PropertyName + " changed on " + sender.GetType());

            PropertyChanged?.Invoke(sender, args);
        }

        #endregion
        #region Implement Undo Stack
        protected void InnerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(sender, e);
        }

        #endregion
        #region Utility
        protected bool SetField<T>([NotNullIfNotNull(nameof(Field))] ref T? Field, T? NewValue, string PropertyName = "", bool ForceRaisePropertyChanged = false)
        {
            //Don't change it if it's already the same.
            T? OldValue = Field;
            if (!EqualityComparer<T>.Default.Equals(Field, NewValue))
            {
                Field = NewValue;

                RaisePropertyChanged(PropertyName);
                return true;
            }

            if (ForceRaisePropertyChanged)
                RaisePropertyChanged(PropertyName);
            return false;
        }

        protected bool SetField<T, TNew>(ref T Field, TNew NewValue, string PropertyName, bool ForceRaisePropertyChanged)
            where T : class
            where TNew : class, T
        {
            return SetField(ref Field, NewValue, PropertyName, ForceRaisePropertyChanged);
        }

        //String has to be a special snowflake. ValueTypes work in the normal GetField fine. Reference types work fine. But STRING doesn't because it has no default constructor, due to being immutable.
        protected static string GetField([System.Diagnostics.CodeAnalysis.NotNull] ref string? Field)
        {
            if (Field == null)
                Field = string.Empty;

            return Field;
        }

        protected static T GetField<T>([System.Diagnostics.CodeAnalysis.NotNull] ref T? Field) where T : new()
        {
            if (Field == null)
            {
                Field = new T();
            }
            return Field;
        }

        #endregion
        #region Implement ICloneable
        public virtual object Clone()
        {
            return new PropertyChangedBase(this, false);
        }
        #endregion
        #region Serialization
        public void SerializeToWriter(StreamWriter DestinationStream, PropertyChangedBase Default, string FieldName, bool WriteFieldNameHeader, bool AsShallow)
        {
            //Do nothing.
        }

        public void AssignPropertyFromStream(StreamReader SourceStream, PropertyChangedBase Default, string FieldName)
        {
            throw new FileFormatException("Deserializer: Tried to deserialize an unrecognized property named " + FieldName);
        }
        #endregion
    }

    public class ExtendedPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public object OldValue;
        public object NewValue;

        public ExtendedPropertyChangedEventArgs(string PropertyName, object OldValue, object NewValue) : base(PropertyName)
        {
            this.OldValue = OldValue;
            this.NewValue = NewValue;
        }
    }

}
