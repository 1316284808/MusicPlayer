using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 频谱数据附加行为 - 用于自动监听和更新频谱数据显示
    /// 将频谱数据更新逻辑从代码后台移到附加行为中
    /// </summary>
    public class SpectrumDataBehavior
    {
        // 注册附加属性
        public static readonly DependencyProperty SpectrumDataProperty =
            DependencyProperty.RegisterAttached(
                "SpectrumData",
                typeof(ObservableCollection<double>),
                typeof(SpectrumDataBehavior),
                new PropertyMetadata(null, OnSpectrumDataChanged));

        public static readonly DependencyProperty IsCircularProperty =
            DependencyProperty.RegisterAttached(
                "IsCircular",
                typeof(bool),
                typeof(SpectrumDataBehavior),
                new PropertyMetadata(false));

        public static readonly DependencyProperty InnerRadiusProperty =
            DependencyProperty.RegisterAttached(
                "InnerRadius",
                typeof(double),
                typeof(SpectrumDataBehavior),
                new PropertyMetadata(125.0));

        public static readonly DependencyProperty OuterRadiusProperty =
            DependencyProperty.RegisterAttached(
                "OuterRadius",
                typeof(double),
                typeof(SpectrumDataBehavior),
                new PropertyMetadata(250.0));

        /// <summary>
        /// 设置频谱数据
        /// </summary>
        public static void SetSpectrumData(DependencyObject element, ObservableCollection<double> value)
        {
            element.SetValue(SpectrumDataProperty, value);
        }

        /// <summary>
        /// 获取频谱数据
        /// </summary>
        public static ObservableCollection<double> GetSpectrumData(DependencyObject element)
        {
            return (ObservableCollection<double>)element.GetValue(SpectrumDataProperty);
        }

        /// <summary>
        /// 设置是否为圆形频谱
        /// </summary>
        public static void SetIsCircular(DependencyObject element, bool value)
        {
            element.SetValue(IsCircularProperty, value);
        }

        /// <summary>
        /// 获取是否为圆形频谱
        /// </summary>
        public static bool GetIsCircular(DependencyObject element)
        {
            return (bool)element.GetValue(IsCircularProperty);
        }

        /// <summary>
        /// 设置内圆半径
        /// </summary>
        public static void SetInnerRadius(DependencyObject element, double value)
        {
            element.SetValue(InnerRadiusProperty, value);
        }

        /// <summary>
        /// 获取内圆半径
        /// </summary>
        public static double GetInnerRadius(DependencyObject element)
        {
            return (double)element.GetValue(InnerRadiusProperty);
        }

        /// <summary>
        /// 设置外圆半径
        /// </summary>
        public static void SetOuterRadius(DependencyObject element, double value)
        {
            element.SetValue(OuterRadiusProperty, value);
        }

        /// <summary>
        /// 获取外圆半径
        /// </summary>
        public static double GetOuterRadius(DependencyObject element)
        {
            return (double)element.GetValue(OuterRadiusProperty);
        }

        /// <summary>
        /// 频谱数据变化处理
        /// </summary>
        private static void OnSpectrumDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ItemsControl itemsControl)
            {
                // 如果之前有数据源，先取消订阅
                if (e.OldValue is ObservableCollection<double> oldData)
                {
                    oldData.CollectionChanged -= (s, args) => UpdateSpectrum(itemsControl);
                }

                // 设置新的数据源
                if (e.NewValue is ObservableCollection<double> newData)
                {
                    itemsControl.ItemsSource = newData;
                    // 订阅数据变化
                    newData.CollectionChanged += (s, args) => UpdateSpectrum(itemsControl);
                    // 初始更新
                    UpdateSpectrum(itemsControl);
                }
            }
        }

        /// <summary>
        /// 更新频谱显示
        /// </summary>
        private static void UpdateSpectrum(ItemsControl itemsControl)
        {
            // 这个方法可以留空，实际的更新逻辑由转换器处理
            // 如果需要额外的处理逻辑，可以在这里实现
        }
    }
}