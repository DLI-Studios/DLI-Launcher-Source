# Graph Report - .  (2026-08-01)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 1321 nodes · 2376 edges · 151 communities (144 shown, 7 thin omitted)
- Extraction: 85% EXTRACTED · 15% INFERRED · 0% AMBIGUOUS · INFERRED: 358 edges (avg confidence: 0.62)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- IFirebaseFirestore
- DLI.Connect.Services.Interfaces
- SessionManager
- FirebaseFirestore
- Window
- UserControl
- UserControl
- UserControl
- CultureInfo
- UserControl
- SettingsViewModel
- MessagesViewModel
- ProfileViewModel
- UserControl
- GeneratedInternalTypeHelper
- App
- UserControl
- VerifyEmailViewModel
- IFriendService
- DLI.Connect.Views
- ISessionManager
- .Log
- UserControl
- FriendRequestsViewModel
- UserControl
- UserControl
- .RefreshChatAsync
- .Navigate
- .RegisterAsync
- FriendsViewModel
- FirebaseClient
- IComponentConnector
- PasswordBox
- ResourceDictionary
- ConversationItemViewModel
- RelayCommand
- LoginView
- RegisterView
- LoginView
- RegisterView
- DisplayName
- MessagesView
- MessagesView
- MessagesView
- InputBox
- SearchResultItemViewModel
- .CropToSquare
- AddFriendsView
- FriendsView
- SettingsView
- AddFriendsView
- FriendsView
- SettingsView
- ViewModelBase
- FriendRequestsView
- ProfileView
- VerifyEmailView
- ForgotPasswordView
- FriendRequestsView
- HomeView
- ProfileView
- VerifyEmailView
- RegisterView
- DLI.Connect.csproj
- .CheckNowAsync
- SettingsCategory
- TextBlock
- ProfileView
- Colors.xaml
- ColorsLight.xaml

## God Nodes (most connected - your core abstractions)
1. `UserControl` - 65 edges
2. `UserControl` - 64 edges
3. `MessagesViewModel` - 60 edges
4. `UserControl` - 57 edges
5. `SettingsViewModel` - 56 edges
6. `ProfileViewModel` - 50 edges
7. `FirebaseFirestore` - 38 edges
8. `DLI.Connect.Views` - 35 edges
9. `UserControl` - 34 edges
10. `ISessionManager` - 33 edges

## Surprising Connections (you probably didn't know these)
- `UserControl` --references--> `CancelRemoveCommand`  [INFERRED]
  Views/FriendsView.xaml → ViewModels/FriendsViewModel.cs
- `UserControl` --references--> `ErrorMessage`  [INFERRED]
  Views/FriendsView.xaml → ViewModels/FriendsViewModel.cs
- `UserControl` --references--> `CloseNewChatCommand`  [INFERRED]
  Views/MessagesView.xaml → ViewModels/MessagesViewModel.cs
- `UserControl` --references--> `DisplayName`  [INFERRED]
  Views/MessagesView.xaml → ViewModels/MessagesViewModel.cs
- `UserControl` --references--> `ErrorMessage`  [INFERRED]
  Views/MessagesView.xaml → ViewModels/MessagesViewModel.cs

## Import Cycles
- None detected.

## Communities (151 total, 7 thin omitted)

### Community 0 - "IFirebaseFirestore"
Cohesion: 0.06
Nodes (24): List, ConversationInfo, FriendRequest, Message, string, Presence, UserNotifications, UserPrivacy (+16 more)

### Community 1 - "DLI.Connect.Services.Interfaces"
Cohesion: 0.06
Nodes (26): DLI.Connect.Services, DLI.Connect.ViewModels, DLI.Connect.Utilities, DLI.Connect.Helpers, DLI.Connect.Firebase, DLI.Connect.Services.Interfaces, DLI.Connect.Models, Dictionary (+18 more)

### Community 2 - "SessionManager"
Cohesion: 0.08
Nodes (15): JsonElement, Task, FirebaseAuth, Task, FirebaseStorage, IFirebaseStorage, string, SessionStorage (+7 more)

### Community 3 - "FirebaseFirestore"
Cohesion: 0.13
Nodes (13): Direction, Field, Dictionary, IReadOnlyList, JsonElement, List, Task, FirebaseFirestore (+5 more)

### Community 4 - "Window"
Cohesion: 0.05
Nodes (40): CloseCommand, CurrentViewModel, MaximizeRestoreCommand, MinimizeCommand, CancelEventArgs, CloseButton, MaximizeButton, MinimizeButton (+32 more)

### Community 5 - "UserControl"
Cohesion: 0.07
Nodes (32): EnumEq, CurrentSection, GoAddFriendsCommand, GoFriendsCommand, GoMessagesCommand, GoOverviewCommand, GoProfileCommand, GoRequestsCommand (+24 more)

### Community 6 - "UserControl"
Cohesion: 0.05
Nodes (41): CloseNewChatCommand, Conversations, Conversations.Count, DataContext.CopyUsernameCommand, DataContext.OpenConversationCommand, DataContext.StartChatCommand, HasOpened, IsNewChatDialogOpen (+33 more)

### Community 7 - "UserControl"
Cohesion: 0.05
Nodes (37): AllCategories, ApplyThemeCommand, CancelDeleteConfirmCommand, Category, ChangePasswordCommand, ConfirmDeleteAccountCommand, DataContext.SelectedCategory, Icon (+29 more)

### Community 8 - "CultureInfo"
Cohesion: 0.12
Nodes (14): IValueConverter, CultureInfo, Type, BooleanToVisibilityConverter, DateToDisplayConverter, EnumEqualsConverter, FirstLetterConverter, HasAvatarToVisibilityConverter (+6 more)

### Community 9 - "UserControl"
Cohesion: 0.06
Nodes (32): AvatarPreview, Bio, CancelAvatarCommand, CancelEditBioCommand, CancelEditNameCommand, HasPendingAvatar, IsEditingBio, IsEditingName (+24 more)

### Community 10 - "SettingsViewModel"
Cohesion: 0.07
Nodes (23): bool, IReadOnlyList, string, SettingsViewModel, ChangePasswordCommand, ConfirmDeleteAccountCommand, ConfirmPassword, CurrentPassword (+15 more)

### Community 11 - "MessagesViewModel"
Cohesion: 0.07
Nodes (27): DateTime, Dictionary, DispatcherTimer, List, ObservableCollection, MessagesViewModel, CloseNewChatCommand, CopyUsernameCommand (+19 more)

### Community 12 - "ProfileViewModel"
Cohesion: 0.07
Nodes (23): byte, ImageSource, bool, string, ProfileViewModel, AvatarPreview, Bio, CancelAvatarCommand (+15 more)

### Community 13 - "UserControl"
Cohesion: 0.08
Nodes (24): AcceptCommand, CancelRemoveCommand, ConfirmRemoveCommand, InvBool, DataContext.RemoveFriendCommand, DeclineCommand, Friends, IsRemoveDialogOpen (+16 more)

### Community 14 - "GeneratedInternalTypeHelper"
Cohesion: 0.11
Nodes (14): XamlGeneratedNamespace, InternalTypeHelper, CultureInfo, Delegate, EventInfo, PropertyInfo, Type, GeneratedInternalTypeHelper (+6 more)

### Community 15 - "App"
Cohesion: 0.10
Nodes (15): Application, Exception, IServiceProvider, App, bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, STAThreadAttribute (+7 more)

### Community 16 - "UserControl"
Cohesion: 0.10
Nodes (24): AddFriendCommand, CanAdd, HasSearched, Results, SearchCommand, StateText, ObservableCollection, string (+16 more)

### Community 17 - "VerifyEmailViewModel"
Cohesion: 0.12
Nodes (18): CheckNowCommand, ResendCommand, CancellationToken, CancellationTokenSource, bool, string, VerifyEmailViewModel, CheckNowCommand (+10 more)

### Community 18 - "IFriendService"
Cohesion: 0.19
Nodes (6): FriendInfo, List, Task, IFriendService, RelayCommand, Task

### Community 19 - "DLI.Connect.Views"
Cohesion: 0.15
Nodes (10): DLI.Connect.Views, DLI.Connect, UserControl, AddFriendsView, ForgotPasswordView, FriendRequestsView, FriendsView, HomeView (+2 more)

### Community 21 - ".Log"
Cohesion: 0.22
Nodes (6): Exception, string, AppErrors, Task, RelayCommand, Task

### Community 22 - "UserControl"
Cohesion: 0.12
Nodes (15): GoToForgotPasswordCommand, GoToRegisterCommand, LoginCommand, RememberMe, DLI.Connect.DesignData, DesignLoginViewModel, EmailBox, PasswordToggle (+7 more)

### Community 23 - "FriendRequestsViewModel"
Cohesion: 0.21
Nodes (9): bool, DispatcherTimer, Func, ObservableCollection, RelayCommand, string, Task, FriendRequestsViewModel (+1 more)

### Community 24 - "UserControl"
Cohesion: 0.17
Nodes (15): EmailSent, GoBackCommand, SendCommand, bool, string, ForgotPasswordViewModel, Email, EmailSent (+7 more)

### Community 25 - "UserControl"
Cohesion: 0.16
Nodes (15): GoToLoginCommand, RegisterCommand, string, RegisterViewModel, ConfirmPassword, ErrorMessage, GoToLoginCommand, Password (+7 more)

### Community 27 - ".Navigate"
Cohesion: 0.20
Nodes (9): AppPage, Func, UserControl, INavigationService, bool, RelayCommand, string, Task (+1 more)

### Community 28 - ".RegisterAsync"
Cohesion: 0.16
Nodes (5): Validators, RelayCommand, Task, RelayCommand, Task

### Community 29 - "FriendsViewModel"
Cohesion: 0.15
Nodes (10): bool, DispatcherTimer, ObservableCollection, RelayCommand, string, Task, FriendsViewModel, CancelRemoveCommand (+2 more)

### Community 30 - "FirebaseClient"
Cohesion: 0.31
Nodes (7): Exception, JsonElement, Task, FirebaseApiException, FirebaseClient, HttpClient, JsonSerializerOptions

### Community 31 - "IComponentConnector"
Cohesion: 0.15
Nodes (9): IComponentConnector, bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, ForgotPasswordView, bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute (+1 more)

### Community 32 - "PasswordBox"
Cohesion: 0.21
Nodes (9): PasswordBox, PasswordBox, ConfirmPasswordBox, PasswordBox, ConfirmPasswordBox, CurrentPasswordBox, NewPasswordBox, RoutedEventArgs (+1 more)

### Community 33 - "ResourceDictionary"
Cohesion: 0.20
Nodes (11): bd, border, box, check, PART_ContentHost, PART_Track, ResourceDictionary, Border (+3 more)

### Community 34 - "ConversationItemViewModel"
Cohesion: 0.20
Nodes (5): long, bool, string, ConversationItemViewModel, MessageItemViewModel

### Community 36 - "LoginView"
Cohesion: 0.20
Nodes (8): bool, Button, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, PasswordBox, TextBlock, TextBox, LoginView

### Community 37 - "RegisterView"
Cohesion: 0.20
Nodes (8): bool, Button, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, PasswordBox, TextBlock, TextBox, RegisterView

### Community 38 - "LoginView"
Cohesion: 0.20
Nodes (8): bool, Button, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, PasswordBox, TextBlock, TextBox, LoginView

### Community 39 - "RegisterView"
Cohesion: 0.20
Nodes (8): bool, Button, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, PasswordBox, TextBlock, TextBox, RegisterView

### Community 40 - "DisplayName"
Cohesion: 0.20
Nodes (10): DisplayName, Email, Username, DisplayName, Username, DisplayNameBox, EmailBox, UsernameBox (+2 more)

### Community 41 - "MessagesView"
Cohesion: 0.28
Nodes (4): DependencyPropertyChangedEventArgs, NotifyCollectionChangedEventArgs, PropertyChangedEventArgs, MessagesView

### Community 42 - "MessagesView"
Cohesion: 0.22
Nodes (7): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, Grid, ScrollViewer, TextBox, MessagesView

### Community 43 - "MessagesView"
Cohesion: 0.22
Nodes (7): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, Grid, ScrollViewer, TextBox, MessagesView

### Community 44 - "InputBox"
Cohesion: 0.25
Nodes (7): MessageText, KeyEventArgs, MessageText, SearchText, InputBox, SearchBox, TextBox

### Community 45 - "SearchResultItemViewModel"
Cohesion: 0.33
Nodes (4): Action, RequestRelationState, bool, SearchResultItemViewModel

### Community 46 - ".CropToSquare"
Cohesion: 0.29
Nodes (4): BitmapSource, Jpeg, Preview, AvatarProcessor

### Community 47 - "AddFriendsView"
Cohesion: 0.29
Nodes (5): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, TextBox, AddFriendsView

### Community 48 - "FriendsView"
Cohesion: 0.29
Nodes (5): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, Grid, FriendsView

### Community 49 - "SettingsView"
Cohesion: 0.29
Nodes (5): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, PasswordBox, SettingsView

### Community 50 - "AddFriendsView"
Cohesion: 0.29
Nodes (5): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, TextBox, AddFriendsView

### Community 51 - "FriendsView"
Cohesion: 0.29
Nodes (5): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, Grid, FriendsView

### Community 52 - "SettingsView"
Cohesion: 0.29
Nodes (5): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, PasswordBox, SettingsView

### Community 53 - "ViewModelBase"
Cohesion: 0.33
Nodes (3): ObservableObject, bool, ViewModelBase

### Community 54 - "FriendRequestsView"
Cohesion: 0.33
Nodes (4): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, FriendRequestsView

### Community 55 - "ProfileView"
Cohesion: 0.33
Nodes (4): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, ProfileView

### Community 56 - "VerifyEmailView"
Cohesion: 0.33
Nodes (4): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, VerifyEmailView

### Community 57 - "ForgotPasswordView"
Cohesion: 0.33
Nodes (4): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, ForgotPasswordView

### Community 58 - "FriendRequestsView"
Cohesion: 0.33
Nodes (4): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, FriendRequestsView

### Community 59 - "HomeView"
Cohesion: 0.33
Nodes (4): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, HomeView

### Community 60 - "ProfileView"
Cohesion: 0.33
Nodes (4): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, ProfileView

### Community 61 - "VerifyEmailView"
Cohesion: 0.33
Nodes (4): bool, DebuggerNonUserCodeAttribute, GeneratedCodeAttribute, VerifyEmailView

### Community 62 - "RegisterView"
Cohesion: 0.33
Nodes (4): Button, PasswordBox, TextBlock, RegisterView

### Community 63 - "DLI.Connect.csproj"
Cohesion: 0.40
Nodes (4): net9.0-windows, CommunityToolkit.Mvvm (8.4.2), Microsoft.Extensions.DependencyInjection (10.0.10), Microsoft.NET.Sdk

### Community 66 - "SettingsCategory"
Cohesion: 0.50
Nodes (3): List, SettingsCategory, SettingsCategoryInfo

### Community 67 - "TextBlock"
Cohesion: 0.50
Nodes (4): PasswordToggleIcon, TextBlock, ConfirmPasswordToggleIcon, PasswordToggleIcon

## Knowledge Gaps
- **187 isolated node(s):** `net9.0-windows`, `CommunityToolkit.Mvvm (8.4.2)`, `Microsoft.Extensions.DependencyInjection (10.0.10)`, `Microsoft.NET.Sdk`, `DLI.Connect.DesignData` (+182 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ISessionManager` connect `ISessionManager` to `IFirebaseFirestore`, `DLI.Connect.Services.Interfaces`, `SessionManager`, `Window`, `UserControl`, `SettingsViewModel`, `MessagesViewModel`, `ProfileViewModel`, `VerifyEmailViewModel`, `UserControl`, `UserControl`, `.Navigate`?**
  _High betweenness centrality (0.197) - this node is a cross-community bridge._
- **Why does `MessagesViewModel` connect `MessagesViewModel` to `IFirebaseFirestore`, `DLI.Connect.Services.Interfaces`, `ConversationItemViewModel`, `UserControl`, `MessagesView`, `InputBox`, `IFriendService`, `ISessionManager`, `ViewModelBase`, `.RefreshChatAsync`?**
  _High betweenness centrality (0.184) - this node is a cross-community bridge._
- **Why does `MessagesView` connect `MessagesView` to `MessagesViewModel`, `DLI.Connect.Views`, `InputBox`, `UserControl`?**
  _High betweenness centrality (0.091) - this node is a cross-community bridge._
- **Are the 18 inferred relationships involving `UserControl` (e.g. with `MessagesViewModel` and `CloseNewChatCommand`) actually correct?**
  _`UserControl` has 18 INFERRED edges - model-reasoned connections that need verification._
- **Are the 18 inferred relationships involving `UserControl` (e.g. with `SettingsViewModel` and `ApplyThemeCommand`) actually correct?**
  _`UserControl` has 18 INFERRED edges - model-reasoned connections that need verification._
- **Are the 25 inferred relationships involving `MessagesViewModel` (e.g. with `CloseNewChatCommand` and `CopyUsernameCommand`) actually correct?**
  _`MessagesViewModel` has 25 INFERRED edges - model-reasoned connections that need verification._
- **Are the 21 inferred relationships involving `UserControl` (e.g. with `ProfileViewModel` and `AvatarPreview`) actually correct?**
  _`UserControl` has 21 INFERRED edges - model-reasoned connections that need verification._