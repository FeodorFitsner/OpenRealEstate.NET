﻿using System;

namespace OpenRealEstate.Core.Models
{
    public class UnitOfMeasure
    {
        private string _type;
        private decimal _value;

        public string Type
        {
            get { return _type; }
            set
            {
                _type = value;
                IsTypeModified = true;
            }
        }

        public bool IsTypeModified { get; private set; }

        public decimal Value
        {
            get { return _value; }
            set
            {
                _value = value;
                IsValueModified = true;
            }
        }

        public bool IsValueModified { get; private set; }

        public bool IsModified
        {
            get
            {
                return IsTypeModified ||
                       IsValueModified;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}",
                Value,
                string.IsNullOrWhiteSpace(Type)
                    ? "-no type-"
                    : Type);
        }

        public void Copy(UnitOfMeasure newUnitOfMeasure)
        {
            if (newUnitOfMeasure == null)
            {
                throw new ArgumentNullException("newUnitOfMeasure");
            }

            if (newUnitOfMeasure.IsTypeModified)
            {
                Type = newUnitOfMeasure.Type;
            }

            if (newUnitOfMeasure.IsValueModified)
            {
                Value = newUnitOfMeasure.Value;
            }
        }

        public virtual void ClearAllIsModified()
        {
            IsTypeModified = false;
            IsValueModified = false;
        }
    }
}