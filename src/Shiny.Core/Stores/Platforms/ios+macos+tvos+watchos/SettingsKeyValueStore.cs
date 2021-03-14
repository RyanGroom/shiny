﻿using System;
using Foundation;
using Shiny.Infrastructure;


namespace Shiny.Stores
{
    public class SettingsKeyValueStore : IKeyValueStore
    {
        readonly ISerializer serializer;
        public SettingsKeyValueStore(ISerializer serializer)
            => this.serializer = serializer;


        public string Alias => "settings";

        public void Clear() => this.Do(prefs =>
        {
            foreach (var key in prefs.ToDictionary())
                prefs.RemoveObject(key.Key.ToString());
        });


        public bool Contains(string key) => this.Get(x => x.ValueForKey(new NSString(key)) != null);
        public object Get(Type type, string key) => this.Get(prefs =>
        {
            var typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return prefs.BoolForKey(key);

                case TypeCode.Double:
                    return prefs.DoubleForKey(key);

                case TypeCode.Int32:
                    return (int)prefs.IntForKey(key);

                case TypeCode.Single:
                    return (float)prefs.FloatForKey(key);

                case TypeCode.String:
                    return prefs.StringForKey(key);

                default:
                    var @string = prefs.StringForKey(key);
                    return this.serializer.Deserialize(type, @string);
            }
        });


        public bool Remove(string key)
        {
            var removed = false;
            if (this.Contains(key))
            {
                this.Do(x => x.RemoveObject(key));
                removed = true;
            }
            return removed;
        }


        public void Set(string key, object value) => this.Do(prefs =>
        {
            var typeCode = Type.GetTypeCode(value.GetType());
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    prefs.SetBool((bool)value, key);
                    break;

                case TypeCode.Double:
                    prefs.SetDouble((double)value, key);
                    break;

                case TypeCode.Int32:
                    prefs.SetInt((int)value, key);
                    break;

                case TypeCode.String:
                    prefs.SetString((string)value, key);
                    break;

                default:
                    var @string = this.serializer.Serialize(value);
                    prefs.SetString(@string, key);
                    break;
            }
        });


        readonly object syncLock = new object();

        protected virtual T Get<T>(Func<NSUserDefaults, T> getter)
        {
            lock (this.syncLock)
            {
                using (var native = NSUserDefaults.StandardUserDefaults)
                    return getter(native);
            }
        }


        protected virtual void Do(Action<NSUserDefaults> action)
        {
            lock (this.syncLock)
            {
                using (var native = NSUserDefaults.StandardUserDefaults)
                {
                    action(native);
                    native.Synchronize();
                }
            }
        }
    }
}


//protected override void NativeSet(Type type, string key, object value) => this.Do(prefs =>
//{

//});