using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using BlazorLazyLoading.Client.Services;
using BlazorLazyLoading.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

//see Microsoft.AspNetCore.Components.Routing.Router

namespace BlazorLazyLoading.Client.Infrastructure
{
    public class LazyRouter : ComponentBase, IComponent, IHandleAfterRender, IDisposable
    {
        static readonly char[] _queryOrHashStartChar = new[] { '?', '#' };
        static readonly ReadOnlyDictionary<string, object> _emptyParametersDictionary
            = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
        private string _locationAbsolute;
        string _baseUri;
        private bool _onNavigateCalled = false;
        private CancellationTokenSource _onNavigateCts;
        private Task _previousOnNavigateTask = Task.CompletedTask;
        RenderHandle _renderHandle;
        bool _navigationInterceptionEnabled;

        [Inject] private INavigationInterception NavigationInterception { get; set; }
        //[Inject] private AuthenticationStateProvider AuthenticationStateProvider
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private RouteService RouteService { get; set; }
        [Inject] private PageService PageService { get; set; }
        [Inject] private ILogger<LazyRouter> Logger { get; set; }


        //TODO: Loading /Navigating definieren
        [Parameter] public RenderFragment Navigating { get; set; }

        /// <summary>
        /// Gets or sets the content to display when no match is found for the requested route.
        /// </summary>
        [Parameter] public RenderFragment NotFound { get; set; }

        /// <summary>
        /// Gets or sets the content to display when a match is found for the requested route.
        /// </summary>
        [Parameter] public RenderFragment<RouteData> Found { get; set; }

        /// <summary>
        /// Gets or sets a handler that should be called before navigating to a new page.
        /// </summary>
        [Parameter] public EventCallback<NavigationContext> OnNavigateAsync { get; set; }

        [CascadingParameter]
        AppState AppState { get; set; }

        [Parameter]
        public Action<AppState> OnStateChange { get; set; }

        //private RenderFragment DynamicComponent { get; set; }

        private List<RouteDefinition> Routes { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
            Logger.LogDebug("Attach");
            //_logger = LoggerFactory.CreateLogger<Router>();
            _renderHandle = renderHandle;
            _baseUri = NavigationManager.BaseUri;
            _locationAbsolute = NavigationManager.Uri;
            NavigationManager.LocationChanged += OnLocationChanged;
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);

            // Found content is mandatory, because even though we could use something like <RouteView ...> as a
            // reasonable default, if it's not declared explicitly in the template then people will have no way
            // to discover how to customize this (e.g., to add authorization).
            if (Found == null)
            {
                throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(Found)}.");
            }

            // NotFound content is mandatory, because even though we could display a default message like "Not found",
            // it has to be specified explicitly so that it can also be wrapped in a specific layout
            if (NotFound == null)
            {
                throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(NotFound)}.");
            }

            if (!_onNavigateCalled)
            {
                _onNavigateCalled = true;
                await RunOnNavigateAsync(NavigationManager.ToBaseRelativePath(_locationAbsolute), isNavigationIntercepted: false);
            }

            await Refresh(isNavigationIntercepted: false);
        }

        internal async ValueTask RunOnNavigateAsync(string path, bool isNavigationIntercepted)
        {
            Logger.LogDebug("RunOnNavigateAsync with path {path}", path);
            // Cancel the CTS instead of disposing it, since disposing does not
            // actually cancel and can cause unintended Object Disposed Exceptions.
            // This effectivelly cancels the previously running task and completes it.
            _onNavigateCts?.Cancel();
            // Then make sure that the task has been completely cancelled or completed
            // before starting the next one. This avoid race conditions where the cancellation
            // for the previous task was set but not fully completed by the time we get to this
            // invocation.
            await _previousOnNavigateTask;

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _previousOnNavigateTask = tcs.Task;

            if (!OnNavigateAsync.HasDelegate)
            {
                Logger.LogDebug("RunOnNavigateAsync - !OnNavigateAsync.HasDelegate");
                await Refresh(isNavigationIntercepted);
            }

            _onNavigateCts = new CancellationTokenSource();
            var navigateContext = new NavigationContext(path, _onNavigateCts.Token);

            var cancellationTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            navigateContext.CancellationToken.Register(state =>
                ((TaskCompletionSource)state).SetResult(), cancellationTcs);

            try
            {
                // Task.WhenAny returns a Task<Task> so we need to await twice to unwrap the exception
                var task = await Task.WhenAny(OnNavigateAsync.InvokeAsync(navigateContext), cancellationTcs.Task);
                await task;
                tcs.SetResult();
                Logger.LogDebug("RunOnNavigateAsync - Refresh(isNavigationIntercepted)");
                await Refresh(isNavigationIntercepted);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "RunOnNavigateAsync - Catch");
                _renderHandle.Render(builder => ExceptionDispatchInfo.Throw(e));
            }
        }



        private void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            Logger.LogDebug("OnLocationChanged {loc}", args.Location);
            _locationAbsolute = args.Location;
            if (_renderHandle.IsInitialized)
            {
                _ = RunOnNavigateAsync(NavigationManager.ToBaseRelativePath(_locationAbsolute), args.IsNavigationIntercepted);
            }
        }

        internal virtual async Task Refresh(bool isNavigationIntercepted)
        {
            Logger.LogDebug("Refresh");
            // If an `OnNavigateAsync` task is currently in progress, then wait
            // for it to complete before rendering. Note: because _previousOnNavigateTask
            // is initialized to a CompletedTask on initialization, this will still
            // allow first-render to complete successfully.
            if (_previousOnNavigateTask.Status != TaskStatus.RanToCompletion)
            {
                Logger.LogDebug("Refresh - _previousOnNavigateTask.Status != TaskStatus.RanToCompletion");
                if (Navigating != null)
                {
                    _renderHandle.Render(Navigating);
                }
                return;
            }

            Logger.LogDebug("Refresh - RouteService.GetRoutes");
            Routes = await RouteService.GetRoutes();

            var locationPath = NavigationManager.ToBaseRelativePath(_locationAbsolute);
            locationPath = StringUntilAny(locationPath, _queryOrHashStartChar);
            if (locationPath == "")
            {
                locationPath = "/";
            }

            Logger.LogDebug("Refresh - locationPath {path}", locationPath);

            //Find fitting type for page
            //TODO: optimize route matching --> RouceContext, RouteEntry, RouteTemplate
            var context = new RouteContext(locationPath);
            var route = Routes.FirstOrDefault(x => x.Path == locationPath);
            Logger.LogDebug("Refresh - type name from route: {page}", route?.TypeFullName);
            var pageType = await PageService.GetPageTypeByRoute(route);
            if (pageType != null)
            {
                Logger.LogDebug("Refresh - page type loaded: {page}", pageType?.FullName);

                if (!typeof(IComponent).IsAssignableFrom(pageType))
                {
                    throw new InvalidOperationException($"The type {pageType.FullName} " +
                                                        $"does not implement {typeof(IComponent).FullName}.");
                }

                Logger.LogDebug("Navigating to {path} and displaying type {type}", locationPath, pageType.FullName);

                var routeData = new RouteData(
                    pageType,
                    _emptyParametersDictionary);
                //context.Parameters ?? _emptyParametersDictionary);
                _renderHandle.Render(Found(routeData));
                return;
            }

            Logger.LogDebug("Refresh - page type null");
            if (!isNavigationIntercepted)
            {
                Logger.LogDebug("Displaying not found for {path}", locationPath);

                // We did not find a Component that matches the route.
                // Only show the NotFound content if the application developer programatically got us here i.e we did not
                // intercept the navigation. In all other cases, force a browser navigation since this could be non-Blazor content.
                _renderHandle.Render(NotFound);
            }
            else
            {
                Logger.LogDebug("Navigating to external url {url} {path}", _locationAbsolute, locationPath);
                NavigationManager.NavigateTo(_locationAbsolute, forceLoad: true);
            }
        }

        private static string StringUntilAny(string str, char[] chars)
        {
            var firstIndex = str.IndexOfAny(chars);
            return firstIndex < 0
                ? str
                : str.Substring(0, firstIndex);
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        Task IHandleAfterRender.OnAfterRenderAsync()
        {
            Logger.LogDebug("OnAfterRenderAsync");
            if (!_navigationInterceptionEnabled)
            {
                _navigationInterceptionEnabled = true;
                return NavigationInterception.EnableNavigationInterceptionAsync();
            }

            return Task.CompletedTask;
        }
    }
}
