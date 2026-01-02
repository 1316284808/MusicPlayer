using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicPlayer.Core.Interface;
using MusicPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Config
{
    /// <summary>
    /// ViewModel生命周期管理器
    /// 负责ViewModel的创建、初始化、清理和销毁
    /// </summary>
    public interface IViewModelLifecycleManager
    {
        /// <summary>
        /// 创建ViewModel实例
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel类型</typeparam>
        /// <returns>ViewModel实例</returns>
        TViewModel CreateViewModel<TViewModel>() where TViewModel : class, IViewModelLifecycle;

        /// <summary>
        /// 创建ViewModel实例（带参数）
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel类型</typeparam>
        /// <param name="parameters">构造函数参数</param>
        /// <returns>ViewModel实例</returns>
        TViewModel CreateViewModel<TViewModel>(params object[] parameters) where TViewModel : class, IViewModelLifecycle;

        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        /// <param name="viewModel">ViewModel实例</param>
        void InitializeViewModel(IViewModelLifecycle viewModel);

        /// <summary>
        /// 清理ViewModel
        /// </summary>
        /// <param name="viewModel">ViewModel实例</param>
        void CleanupViewModel(IViewModelLifecycle viewModel);

        /// <summary>
        /// 销毁ViewModel并释放资源
        /// </summary>
        /// <param name="viewModel">ViewModel实例</param>
        void DestroyViewModel(IViewModelLifecycle viewModel);

        /// <summary>
        /// 注册ViewModel到生命周期管理
        /// </summary>
        /// <param name="viewModel">ViewModel实例</param>
        /// <param name="owner">所有者（通常是Window或UserControl）</param>
        void RegisterViewModel(IViewModelLifecycle viewModel, object? owner = null);

        /// <summary>
        /// 从生命周期管理中注销ViewModel
        /// </summary>
        /// <param name="viewModel">ViewModel实例</param>
        void UnregisterViewModel(IViewModelLifecycle viewModel);

        /// <summary>
        /// 清理指定所有者的所有ViewModel
        /// </summary>
        /// <param name="owner">所有者</param>
        void CleanupViewModelsForOwner(object owner);

        /// <summary>
        /// 清理所有注册的ViewModel
        /// </summary>
        void CleanupAllViewModels();
    }

    /// <summary>
    /// ViewModel生命周期管理器实现
    /// </summary>
    public class ViewModelLifecycleManager : IViewModelLifecycleManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ViewModelLifecycleManager> _logger;
        private readonly Dictionary<IViewModelLifecycle, object?> _registeredViewModels;
        private readonly object _lockObject = new();

        public ViewModelLifecycleManager(IServiceProvider serviceProvider, ILogger<ViewModelLifecycleManager> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registeredViewModels = new Dictionary<IViewModelLifecycle, object?>();
        }

        public TViewModel CreateViewModel<TViewModel>() where TViewModel : class, IViewModelLifecycle
        {
            try
            {
                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
                _logger.LogDebug($"Created ViewModel: {typeof(TViewModel).Name}");
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create ViewModel: {typeof(TViewModel).Name}");
                throw;
            }
        }

        public TViewModel CreateViewModel<TViewModel>(params object[] parameters) where TViewModel : class, IViewModelLifecycle
        {
            try
            {
                // 尝试通过DI容器获取
                var viewModel = _serviceProvider.GetService<TViewModel>();
                
                if (viewModel == null)
                {
                    // 如果DI容器中没有，尝试使用反射创建
                    var constructor = typeof(TViewModel).GetConstructors()
                        .FirstOrDefault(c => c.GetParameters().Length == parameters.Length);

                    if (constructor == null)
                    {
                        throw new InvalidOperationException($"No suitable constructor found for {typeof(TViewModel).Name} with {parameters.Length} parameters");
                    }

                    viewModel = (TViewModel)Activator.CreateInstance(typeof(TViewModel), parameters)!;
                }

                _logger.LogDebug($"Created ViewModel with parameters: {typeof(TViewModel).Name}");
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create ViewModel with parameters: {typeof(TViewModel).Name}");
                throw;
            }
        }

        public void InitializeViewModel(IViewModelLifecycle viewModel)
        {
            if (viewModel == null)
            {
                _logger.LogWarning("Attempted to initialize null ViewModel");
                return;
            }

            try
            {
                // 如果是MainViewModel，不调用子ViewModel的初始化，因为已经处理过了
                if (viewModel is MainViewModel mainViewModel)
                {
                    mainViewModel.Initialize();
                }
                else
                {
                    // 对于其他ViewModel，直接调用初始化
                    viewModel.Initialize();
                }
                
                _logger.LogDebug($"Initialized ViewModel: {viewModel.GetType().Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to initialize ViewModel: {viewModel.GetType().Name}");
                throw;
            }
        }

        public void CleanupViewModel(IViewModelLifecycle viewModel)
        {
            if (viewModel == null)
            {
                _logger.LogWarning("Attempted to cleanup null ViewModel");
                return;
            }

            try
            {
                viewModel.Cleanup();
                _logger.LogDebug($"Cleaned up ViewModel: {viewModel.GetType().Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to cleanup ViewModel: {viewModel.GetType().Name}");
            }
        }

        public void DestroyViewModel(IViewModelLifecycle viewModel)
        {
            if (viewModel == null)
            {
                _logger.LogWarning("Attempted to destroy null ViewModel");
                return;
            }

            try
            {
                // 先清理
                CleanupViewModel(viewModel);

                // 如果实现了IDisposable，释放资源
                if (viewModel is IDisposable disposable)
                {
                    disposable.Dispose();
                    _logger.LogDebug($"Disposed ViewModel: {viewModel.GetType().Name}");
                }

                // 从注册表中移除
                UnregisterViewModel(viewModel);

                _logger.LogDebug($"Destroyed ViewModel: {viewModel.GetType().Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to destroy ViewModel: {viewModel.GetType().Name}");
            }
        }

        public void RegisterViewModel(IViewModelLifecycle viewModel, object? owner = null)
        {
            if (viewModel == null)
            {
                _logger.LogWarning("Attempted to register null ViewModel");
                return;
            }

            lock (_lockObject)
            {
                if (_registeredViewModels.ContainsKey(viewModel))
                {
                    _logger.LogWarning($"ViewModel {viewModel.GetType().Name} is already registered");
                    return;
                }

                _registeredViewModels[viewModel] = owner;
                _logger.LogDebug($"Registered ViewModel: {viewModel.GetType().Name}, Owner: {owner?.GetType().Name ?? "None"}");
            }
        }

        public void UnregisterViewModel(IViewModelLifecycle viewModel)
        {
            if (viewModel == null)
            {
                _logger.LogWarning("Attempted to unregister null ViewModel");
                return;
            }

            lock (_lockObject)
            {
                if (_registeredViewModels.Remove(viewModel))
                {
                    _logger.LogDebug($"Unregistered ViewModel: {viewModel.GetType().Name}");
                }
                else
                {
                    _logger.LogWarning($"ViewModel {viewModel.GetType().Name} was not registered");
                }
            }
        }

        public void CleanupViewModelsForOwner(object owner)
        {
            if (owner == null)
            {
                _logger.LogWarning("Attempted to cleanup ViewModels for null owner");
                return;
            }

            lock (_lockObject)
            {
                var viewModelsToCleanup = _registeredViewModels
                    .Where(kvp => ReferenceEquals(kvp.Value, owner))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var viewModel in viewModelsToCleanup)
                {
                    try
                    {
                        DestroyViewModel(viewModel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to cleanup ViewModel: {viewModel.GetType().Name} for owner: {owner.GetType().Name}");
                    }
                }

                _logger.LogDebug($"Cleaned up {viewModelsToCleanup.Count} ViewModels for owner: {owner.GetType().Name}");
            }
        }

        public void CleanupAllViewModels()
        {
            lock (_lockObject)
            {
                var viewModelsToCleanup = _registeredViewModels.Keys.ToList();

                foreach (var viewModel in viewModelsToCleanup)
                {
                    try
                    {
                        DestroyViewModel(viewModel);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to cleanup ViewModel: {viewModel.GetType().Name}");
                    }
                }

                _logger.LogDebug($"Cleaned up all {viewModelsToCleanup.Count} registered ViewModels");
            }
        }
    }

    /// <summary>
    /// ViewModel生命周期接口
    /// </summary>
    public interface IViewModelLifecycle
    {
        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        void Initialize();

        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        void Cleanup();
    }
}