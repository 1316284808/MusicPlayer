using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using MusicPlayer.Services.Messages;
using MusicPlayer.Services;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 圆形频谱路径转换器 - 将频谱数据值转换为Path元素
    /// 支持多种参数配置，通过ConverterParameter传递配置信息
    /// </summary>
    public class CircularSpectrumPathConverter : IMultiValueConverter, IDisposable
    {
        // 缓存机制：存储固定计算结果，避免重复计算
        private Dictionary<Tuple<int, double, double, double, double, int>, Tuple<double, double, double, double>> _angleCache = new Dictionary<Tuple<int, double, double, double, double, int>, Tuple<double, double, double, double>>();
        
        /// <summary>
        /// 初始化转换器，订阅频谱显示状态变化
        /// </summary>
        public static void Initialize(Core.Interface.IMessagingService messagingService)
        {
           
        }
        
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length >= 7 && values[0] is double spectrumValue && values[1] is int index)
                {
                    // 从ViewModel获取参数
                    double centerX = values[2] is double cx ? cx : 450;
                    double centerY = values[3] is double cy ? cy : 450;
                    double innerRadius = values[4] is double ir ? ir : 225;
                    double maxBarHeight = values[5] is double mh ? mh : 225;
                    int totalCount = values[6] is int tc ? tc : 32;
                    // 计算当前频谱条的角度（从顶部开始，顺时针）
                    double angleStep = 360.0 / totalCount;
                    double currentAngle = (index * angleStep - 90) * Math.PI / 180; // 转换为弧度，-90度使第一个条从顶部开始
                  
                    // 计算频谱条的实际高度（考虑音频强度）
                    double barHeight = Math.Max(5, spectrumValue * maxBarHeight);
                    double outerRadius = innerRadius + barHeight;
                    
                    // 计算频谱条的起始和结束点
                    double angleWidth = angleStep * 60; // 扇形频谱 90 120 180 
                    
                    // 创建缓存键
                    var cacheKey = Tuple.Create(index, centerX, centerY, innerRadius, maxBarHeight, totalCount);
                    Tuple<double, double, double, double> cachedAngles;
                    
                    // 尝试从缓存中获取角度，只在固定参数变化时重新计算
                    if (!_angleCache.TryGetValue(cacheKey, out cachedAngles))
                    {
                        double tempStartAngle = currentAngle - (angleWidth * Math.PI / 360);
                        double tempEndAngle = currentAngle + (angleWidth * Math.PI / 360);
                        cachedAngles = Tuple.Create(tempStartAngle, tempEndAngle, Math.Cos(tempStartAngle), Math.Sin(tempStartAngle));
                        _angleCache[cacheKey] = cachedAngles;
                    }
                    
                    double startAngle = cachedAngles.Item1;
                    double endAngle = cachedAngles.Item2;
                    double cosStart = cachedAngles.Item3;
                    double sinStart = cachedAngles.Item4;
                    
                    // 预计算常用三角函数值
                    double cosEnd = Math.Cos(endAngle);
                    double sinEnd = Math.Sin(endAngle);
                    
                    // 计算扇形的四个点
                    double x1 = centerX + innerRadius * cosStart;
                    double y1 = centerY + innerRadius * sinStart;
                    double x2 = centerX + innerRadius * cosEnd;
                    double y2 = centerY + innerRadius * sinEnd;
                    double x3 = centerX + outerRadius * cosEnd;
                    double y3 = centerY + outerRadius * sinEnd;
                    double x4 = centerX + outerRadius * cosStart;
                    double y4 = centerY + outerRadius * sinStart;
                    
                    // 创建扇形路径
                    var pathFigures = new PathFigureCollection();
                    
                    // 从内圆起点开始
                    var pathFigure = new PathFigure
                    {
                        StartPoint = new Point(x1, y1)
                    };
                    
                    // 内圆弧（顺时针）
                    var arcSegment1 = new ArcSegment
                    {
                        Point = new Point(x2, y2),
                        Size = new Size(innerRadius, innerRadius),
                        RotationAngle = 0,
                        IsLargeArc = false,
                        SweepDirection = SweepDirection.Clockwise
                    };
                    
                    // 连接到外圆终点
                    var lineSegment1 = new LineSegment(new Point(x3, y3), false);
                    
                    // 外圆弧（顺时针回到起点）
                    var arcSegment2 = new ArcSegment
                    {
                        Point = new Point(x4, y4),
                        Size = new Size(outerRadius, outerRadius),
                        RotationAngle = 0,
                        IsLargeArc = false,
                        SweepDirection = SweepDirection.Clockwise
                    };
                    
                    // 闭合路径
                    var lineSegment2 = new LineSegment(new Point(x1, y1), false);
                    
                    // 添加所有段到路径
                    pathFigure.Segments.Add(arcSegment1);
                    pathFigure.Segments.Add(lineSegment1);
                    pathFigure.Segments.Add(arcSegment2);
                    pathFigure.Segments.Add(lineSegment2);
                    
                    pathFigures.Add(pathFigure);
                    
                    return new PathGeometry(pathFigures);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CircularSpectrumPathConverter Error: {ex.Message}");
            }
            
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        
        public void Dispose()
        {
            // 不需要实例级别的清理，使用静态Cleanup方法
        }
    }

    /// <summary>
    /// 圆形频谱颜色转换器 - 根据频谱索引和强度计算颜色
    /// </summary>
    public class CircularSpectrumColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length >= 3 && 
                    values[0] is int index && 
                    values[1] is double intensity &&
                    values[2] is int totalCount)
                {
                    // 计算当前扇形在圆形中的位置比例
                    double positionRatio = (double)index / totalCount;
                    
                    // 根据位置选择不同的色系
                    int colorScheme = (int)(positionRatio * 6); // 分为6个色系
                    
                    // 定义基础颜色和目标颜色的RGB值
                    byte[] baseColors = new byte[3];
                    byte[] targetColors = new byte[3];
                    
                    switch (colorScheme % 6)
                    {
                        case 4: // 蓝色系
                            baseColors = new byte[] { 0, 50, 200 };    // 深蓝
                            targetColors = new byte[] { 0, 150, 255 };  // 亮蓝
                            break;
                        case 3: // 青色系
                            baseColors = new byte[] { 0, 150, 150 };   // 青色
                            targetColors = new byte[] { 0, 255, 200 };  // 亮青
                            break;
                        case 5: // 绿色系
                            baseColors = new byte[] { 0, 150, 50 };    // 深绿
                            targetColors = new byte[] { 50, 255, 100 }; // 亮绿
                            break;
                        case 2: // 黄色系
                            baseColors = new byte[] { 200, 150, 0 };   // 金黄
                            targetColors = new byte[] { 255, 220, 0 }; // 亮黄
                            break;
                        case 0: // 红色系
                            baseColors = new byte[] { 200, 50, 50 };    // 深红
                            targetColors = new byte[] { 255, 100, 100 }; // 亮红
                            break;
                        case 1: // 紫色系
                            baseColors = new byte[] { 150, 50, 200 };  // 深紫
                            targetColors = new byte[] { 220, 100, 255 }; // 亮紫
                            break;
                        default:
                            baseColors = new byte[] { 0, 120, 215 };    // 默认蓝
                            targetColors = new byte[] { 0, 255, 255 }; // 默认青
                            break;
                    }
                    
                    // 使用线性插值计算当前强度下的颜色
                    intensity = Math.Max(0, Math.Min(1, intensity)); // 确保强度在0-1范围内
                    byte r = (byte)(baseColors[0] * (1 - intensity) + targetColors[0] * intensity);
                    byte g = (byte)(baseColors[1] * (1 - intensity) + targetColors[1] * intensity);
                    byte b = (byte)(baseColors[2] * (1 - intensity) + targetColors[2] * intensity);
                    
                // 返回带有透明度的画刷
                Color color = Color.FromArgb(200, r, g, b);
                return new SolidColorBrush(color);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CircularSpectrumColorConverter Error: {ex.Message}");
            }
            
            return Colors.Blue; // 默认颜色
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        
        public void Dispose()
        {
            // 不需要实例级别的清理，使用静态Cleanup方法
        }
    }
}