﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWP.Chart.Common;
using UWP.Chart.Util;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace UWP.Chart
{
    public class Series : ModelBase, ISeries
    {
        #region Private Property
        protected double[,] values = null, previousValues = null;
        protected List<object> dependentValues = new List<object>();
        protected bool[] isTimeValues;
        #endregion

        #region Internal Property
        internal static double NullValue = double.NaN;
        #endregion

        #region Public Property
        private Binding _dependentValueBinding;
        /// <summary>
        /// The binding used to identify the dependent value binding.
        /// </summary>
        public Binding DependentValueBinding
        {
            get
            {
                return _dependentValueBinding;
            }
            set
            {
                if (_dependentValueBinding != value)
                {
                    _dependentValueBinding = value;
                    OnPropertyChanged("DependentValueBinding");
                }
            }
        }

        /// <summary>
        /// Gets or sets the Binding Path to use for identifying the dependent value.
        /// </summary>
        public string DependentValuePath
        {
            get
            {
                return (null != DependentValueBinding) ? DependentValueBinding.Path.Path : null;
            }
            set
            {
                if (null == value)
                {
                    DependentValueBinding = null;
                }
                else
                {
                    DependentValueBinding = new Binding() { Path = new PropertyPath(value) };
                }
            }
        }


        #endregion

        #region DependencyProperty
        /// <summary>
        /// Gets or sets the values(Y) collection.
        /// </summary>
        public DoubleCollection DependentValues
        {
            get { return (DoubleCollection)GetValue(DependentValuesProperty); }
            set { SetValue(DependentValuesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DependentValues.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DependentValuesProperty =
            DependencyProperty.Register("DependentValues", typeof(DoubleCollection), typeof(Series), new PropertyMetadata(null, OnDependencyPropertyChangedToInvalidate));

        /// <summary>
        /// Gets or sets the dependent values(Y) source.
        /// </summary>
        public IEnumerable DependentValuesSource
        {
            get { return (IEnumerable)GetValue(DependentValuesSourceProperty); }
            set { SetValue(DependentValuesSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DependentValuesSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DependentValuesSourceProperty =
            DependencyProperty.Register("DependentValuesSource", typeof(IEnumerable), typeof(Series), new PropertyMetadata(null, OnDependencyPropertyChangedToInvalidate));


        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(Series), new PropertyMetadata("", OnDependencyPropertyChangedToInvalidate));


        #endregion


        #region ISeries

        string[] ISeries.GetItemNames()
        {
            return GetItemNames();
        }

        double[,] ISeries.GetValues()
        {
            return GetValues();
        }

        virtual internal string[] GetItemNames()
        {
            throw new NotImplementedException();
        }

        virtual internal double[,] GetValues()
        {
            if (values != null)
            {
                return values;
            }

            if (DependentValueBinding == null)
                InitList(dependentValues, DependentValuesSource, DependentValues);


            values = CreateValues(new IList[] { dependentValues });

            if (isTimeValues == null)
                isTimeValues = new bool[1];
            isTimeValues[0] = IsTimeData(dependentValues);

            return values;
        }

        #endregion

        #region Common

        protected bool IsTimeData(List<object> list)
        {
            return (list.Count > 0) && (list[0] is DateTime);
        }

        internal static int GetMaxCount(params IList[] list)
        {
            int cnt = 0;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].Count > cnt)
                    cnt = list[i].Count;
            }

            return cnt;
        }

        internal double[,] CreateValues(IList[] lists)
        {
            double[,] values = null;
            if (lists != null)
            {
                int nlists = lists.Length;

                if (nlists > 0)
                {
                    int npts = GetMaxCount(lists);

                    if (npts > 0)
                    {
                        values = new double[nlists, npts];

                        for (int il = 0; il < nlists; il++)
                        {
                            if (lists[il] is double[])
                            {
                                double[] arr = (double[])lists[il];
                                int len = arr.Length;
                                for (int i = 0; i < len; i++)
                                    values[il, i] = arr[i];
                            }
                            else if (lists[il] is float[])
                            {
                                float[] arr = (float[])lists[il];
                                int len = arr.Length;
                                for (int i = 0; i < len; i++)
                                    values[il, i] = arr[i];
                            }
                            else
                            {
                                IList list = lists[il];

                                if (list != null)
                                {
                                    int lcnt = list.Count;
                                    for (int ip = 0; ip < npts; ip++)
                                    {
                                        if (ip < lcnt)
                                            values[il, ip] = ConvertDouble(list[ip], ip);
                                        else
                                            values[il, ip] = NullValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return values;
        }


        internal static void InitList(List<object> list, IEnumerable vals, IList<double> coll)
        {
            list.Clear();
            if (vals != null)
            {
                var ilist = vals as IList;

                if (ilist != null)
                {
                    var cnt = ilist.Count;

                    if (list.Capacity < cnt)
                        list.Capacity = cnt;

                    for (int i = 0; i < cnt; i++)
                        list.Add(ilist[i]);
                }
                else
                {
                    IEnumerator en = vals.GetEnumerator();
                    DataUtils.TryReset(en);
                    while (en.MoveNext())
                        list.Add(en.Current);
                }
            }
            else if (coll != null)
            {
                int cnt = coll.Count;
                for (int i = 0; i < cnt; i++)
                    list.Add(coll[i]);
            }
        }

        internal static double ConvertDouble(object obj, double index)
        {
            double val = double.NaN;

            if (obj == null)
                val = double.NaN;
            else if (obj is DateTime)
            {
                val = ((DateTime)obj).ToOADate();
            }
            else
            {
                string s = obj as string;
                if (s != null)
                {
                    if (!double.TryParse(s, out val))
                    {
                        DateTime dt = DateTime.MinValue;

                        if (DateTime.TryParse(s, out dt))
                            val = dt.ToOADate();
                        else
                            val = index;
                    }

                }
                else
                {
                    try
                    {
                        val = Convert.ToDouble(obj, CultureInfo.CurrentCulture);
                    }
                    catch (InvalidCastException)
                    {
                        val = double.NaN;
                    }
                }
            }

            return val;
        }

        #endregion

    }
}
