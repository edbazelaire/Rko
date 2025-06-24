using Data.GameManagement;
using Enums;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Data
{
    [Serializable]
    public struct SAnalyticsFilter
    {
        public EAnalyticsParam AnalyticParam;
        public string Value;
        public EComparator Comparator;

        public SAnalyticsFilter(EAnalyticsParam analyticParam, string value, EComparator comparator = EComparator.Equal)
        {
            AnalyticParam = analyticParam;
            Value = value;
            Comparator = comparator;
        }

        public bool Check(object value, bool validateOnNull = false)
        {
            // CHECK : null value provided
            if (value == null)
                return validateOnNull;

            // CHECK :  equals first
            if (Comparator == EComparator.Equal)
                return Value.Equals(value.ToString());

            // CHECK : is numerical value
            if (!float.TryParse(value.ToString(), out float valueToCheck))
            {
                ErrorHandler.Error($"Using Comparator {Comparator} on a non numerical provided value {value}");
                return false;
            }

            if (!float.TryParse(Value, out float filterValue))
            {
                ErrorHandler.Error($"Using Comparator {Comparator} on a non numerical filter value {Value}");
                return false;
            }

            // switch case comparator
            switch (Comparator)
            {
                case EComparator.Inf:
                    return valueToCheck < filterValue;

                case EComparator.InfEq:
                    return valueToCheck <= filterValue;

                case EComparator.SupEq:
                    return valueToCheck > filterValue;

                case EComparator.Sup:
                    return valueToCheck >= filterValue;

                default:
                    ErrorHandler.Error("Unhandled case : " + Comparator);
                    return false;
            }
        }
    }
}