using System;

namespace NeverSerender.UserInterface.Elements
{
    public class ElementProperty
    {
        /// <summary>
        /// This event is triggered when the underlying value is changed from outside.
        /// It will not be triggered when the value is changed through the property setter.
        /// </summary>
        public event Action Changed;
        
        public void Notify() => Changed?.Invoke();
    }
    
    public class ElementProperty<T> : ElementProperty
    {
        private readonly Func<T> getter;
        private readonly Action<T> setter;
        
        public ElementProperty(Func<T> getter, Action<T> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public T Get() => getter();
        public void Set(T value) => setter(value);
    }
}