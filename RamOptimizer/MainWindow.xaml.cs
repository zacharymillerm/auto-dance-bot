using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using WindowsInput;
using WindowsInput.Native;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace RamOptimizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Windows API constants
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;
        
        // Modifiers
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        
        // Virtual Keys
        private const uint VK_F12 = 0x7B;

        // Windows API imports
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer countdownTimer = new DispatcherTimer();
        private InputSimulator inputSimulator = null!;
        private Random random = null!;
        private bool isRunning = false;
        private bool isDarkTheme = false;
        private int timeRangeSeconds;
        private int remainingSeconds;
        private NotifyIcon notifyIcon = null!;
        private HwndSource? hwndSource;
        private int superCleanIntervalCounter = 0;
        private int superCleanArrowDownThreshold = 0;

        public MainWindow()
        {
            InitializeComponent();
            inputSimulator = new InputSimulator();
            random = new Random();
            InitializeTimer();
            InitializeTrayIcon();
            this.StateChanged += MainWindow_StateChanged;
            this.Loaded += MainWindow_Loaded;
        }

        private void InitializeTimer()
        {
            timer.Tick += Timer_Tick;
            
            countdownTimer.Interval = TimeSpan.FromSeconds(1);
            countdownTimer.Tick += CountdownTimer_Tick;
        }

        private void InitializeTrayIcon()
        {
            notifyIcon = new NotifyIcon();
            
            // Get the full path to the icon file
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dance.ico");
            if (File.Exists(iconPath))
            {
                notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            }
            else
            {
                // Fallback: try to load from assembly resources or use default icon
                try
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith("dance.ico"));
                    if (resourceName != null)
                    {
                        using (var stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream != null)
                            {
                                notifyIcon.Icon = new System.Drawing.Icon(stream);
                            }
                        }
                    }
                }
                catch
                {
                    // If all else fails, use a default system icon
                    notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            
            notifyIcon.Visible = false;
            notifyIcon.Text = "RAM Optimizer";
            
            // Create context menu
            var contextMenu = new ContextMenuStrip();
            var showMenuItem = new ToolStripMenuItem("Show");
            showMenuItem.Click += (s, e) => ShowWindow();
            var exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += (s, e) => System.Windows.Application.Current.Shutdown();
            
            contextMenu.Items.Add(showMenuItem);
            contextMenu.Items.Add(exitMenuItem);
            notifyIcon.ContextMenuStrip = contextMenu;
            
            // Double click to restore
            notifyIcon.DoubleClick += (s, e) => ShowWindow();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Register global hotkey (Ctrl + Shift + F12)
            var helper = new WindowInteropHelper(this);
            hwndSource = HwndSource.FromHwnd(helper.Handle);
            hwndSource.AddHook(HwndHook);
            RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_F12);
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(1000, "RAM Optimizer", "Application minimized to tray", ToolTipIcon.Info);
            }
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            notifyIcon.Visible = false;
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                // Toggle start/stop optimization
                if (!isRunning)
                {
                    StartOptimization();
                }
                else
                {
                    StopOptimization();
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!isRunning) return;

            try
            {
                // Super Clean mode - only scroll down and occasional arrow down
                if (chkSuperClean.IsChecked == true)
                {
                    SimulateSuperCleanMode();
                }
                else
                {
                    // Simulate keyboard events
                    if (chkKeyboard.IsChecked == true)
                    {
                        SimulateKeyboardEvent();
                    }

                    // Simulate mouse events
                    if (chkMouse.IsChecked == true)
                    {
                        SimulateMouseEvent();
                    }

                    // Simulate mouse movement
                    if (chkMouseMove.IsChecked == true)
                    {
                        SimulateMouseMovement();
                    }
                }
                
                // Add human-like randomization to next interval (±10% variation)
                int variation = random.Next(-timeRangeSeconds / 10, timeRangeSeconds / 10 + 1);
                int nextInterval = Math.Max(timeRangeSeconds + variation, 5); // Minimum 5 seconds
                timer.Interval = TimeSpan.FromSeconds(nextInterval);
                
                // Reset countdown after action
                remainingSeconds = nextInterval;
            }
            catch (Exception)
            {
                // Error occurred during simulation
            }
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            if (!isRunning) return;
            
            remainingSeconds--;
        }

        private void SimulateKeyboardEvent()
        {
            // Add human-like delay variation before keystroke
            System.Threading.Thread.Sleep(random.Next(10, 50));

            // Randomly choose between Page Up and Page Down
            // Add more human-like behavior with occasional double presses
            VirtualKeyCode key = random.Next(2) == 0 ? VirtualKeyCode.PRIOR : VirtualKeyCode.NEXT;
            
            inputSimulator.Keyboard.KeyPress(key);
            
            // Occasionally do a double press (like humans sometimes do)
            if (random.Next(10) == 0)
            {
                System.Threading.Thread.Sleep(random.Next(50, 150));
                inputSimulator.Keyboard.KeyPress(key);
            }
        }

        private void SimulateMouseEvent()
        {
            // Add human-like delay variation
            System.Threading.Thread.Sleep(random.Next(10, 50));

            // Randomly choose between scroll up and scroll down
            // Human-like scroll: variable amounts with occasional pauses
            int scrollAmount = random.Next(1, 4); // Smaller, more natural scroll
            int direction = random.Next(2) == 0 ? 1 : -1;
            
            // Simulate gradual scrolling (more human-like)
            for (int i = 0; i < scrollAmount; i++)
            {
                inputSimulator.Mouse.VerticalScroll(direction);
                if (scrollAmount > 1)
                {
                    System.Threading.Thread.Sleep(random.Next(20, 80));
                }
            }
        }

        private void SimulateMouseMovement()
        {
            // Human-like mouse movement: smooth curves instead of instant jumps
            int targetDeltaX = random.Next(-30, 31); // Smaller range for more natural movement
            int targetDeltaY = random.Next(-15, 16);
            
            // Number of steps for smooth movement
            int steps = random.Next(5, 15);
            
            for (int i = 0; i < steps; i++)
            {
                // Use easing function for more natural acceleration/deceleration
                double progress = (double)(i + 1) / steps;
                double easing = EaseInOutQuad(progress);
                
                int stepX = (int)(targetDeltaX * easing / steps);
                int stepY = (int)(targetDeltaY * easing / steps);
                
                inputSimulator.Mouse.MoveMouseBy(stepX, stepY);
                
                // Variable delay between steps (humans don't move at constant speed)
                System.Threading.Thread.Sleep(random.Next(5, 20));
            }
        }

        private double EaseInOutQuad(double t)
        {
            // Quadratic easing function for smooth acceleration/deceleration
            return t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
        }

        private void SimulateSuperCleanMode()
        {
            // Add human-like delay variation
            System.Threading.Thread.Sleep(random.Next(10, 50));

            // Scroll down 3-8 lines
            int scrollLines = random.Next(3, 9); // 3 to 8 lines
            
            // Simulate gradual scrolling (more human-like)
            for (int i = 0; i < scrollLines; i++)
            {
                inputSimulator.Mouse.VerticalScroll(-1); // -1 for scroll down
                System.Threading.Thread.Sleep(random.Next(20, 80));
            }

            // Increment interval counter
            superCleanIntervalCounter++;

            // Check if we need to send arrow down (every 6-10 intervals)
            if (superCleanIntervalCounter >= superCleanArrowDownThreshold)
            {
                // Add small delay before arrow down
                System.Threading.Thread.Sleep(random.Next(100, 300));
                
                // Send arrow down key press
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.DOWN);
                
                // Reset counter and set new random threshold (6-10 intervals)
                superCleanIntervalCounter = 0;
                superCleanArrowDownThreshold = random.Next(6, 11); // 6 to 10
            }
        }


        private void BtnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                StartOptimization();
            }
            else
            {
                StopOptimization();
            }
        }

        private void StartOptimization()
        {
            // Validate input
            if (!int.TryParse(txtTimeRange.Text, out int timeRange) || timeRange < 10)
            {
                System.Windows.MessageBox.Show("Please enter a valid number for time range (minimum 10 seconds).", "Invalid Input", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (chkKeyboard.IsChecked != true && chkMouse.IsChecked != true && chkMouseMove.IsChecked != true && chkSuperClean.IsChecked != true)
            {
                System.Windows.MessageBox.Show("Please select at least one option (Keyboard, Mouse Scroll, Mouse Movement, or Super Clean).", "No Option Selected", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Initialize Super Clean mode counters
            superCleanIntervalCounter = 0;
            superCleanArrowDownThreshold = random.Next(6, 11); // 6 to 10 intervals

            // Initialize countdown
            timeRangeSeconds = timeRange;
            remainingSeconds = timeRange;

            // Start the timers
            timer.Interval = TimeSpan.FromSeconds(timeRange);
            timer.Start();
            countdownTimer.Start();
            isRunning = true;

            // Update UI
            btnStartStop.Content = "Stop";
            btnStartStop.Background = new SolidColorBrush(Colors.Gray);
            
            // Disable controls
            chkKeyboard.IsEnabled = false;
            chkMouse.IsEnabled = false;
            chkMouseMove.IsEnabled = false;
            chkSuperClean.IsEnabled = false;
            txtTimeRange.IsEnabled = false;
        }

        private void StopOptimization()
        {
            // Stop the timers
            timer.Stop();
            countdownTimer.Stop();
            isRunning = false;

            // Update UI
            btnStartStop.Content = "Start";
            btnStartStop.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green

            // Enable controls (respect Super Clean mode state)
            bool isSuperCleanChecked = chkSuperClean.IsChecked == true;
            chkKeyboard.IsEnabled = !isSuperCleanChecked;
            chkMouse.IsEnabled = !isSuperCleanChecked;
            chkMouseMove.IsEnabled = !isSuperCleanChecked;
            chkSuperClean.IsEnabled = true;
            txtTimeRange.IsEnabled = true;
        }

        private void TxtTimeRange_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only numeric input
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ChkSuperClean_Changed(object sender, RoutedEventArgs e)
        {
            bool isSuperCleanChecked = chkSuperClean.IsChecked == true;
            
            // Disable/Enable other checkboxes based on Super Clean state
            chkKeyboard.IsEnabled = !isSuperCleanChecked;
            chkMouse.IsEnabled = !isSuperCleanChecked;
            chkMouseMove.IsEnabled = !isSuperCleanChecked;
            
            // Optionally uncheck the other options when Super Clean is selected
            if (isSuperCleanChecked)
            {
                chkKeyboard.IsChecked = false;
                chkMouse.IsChecked = false;
                chkMouseMove.IsChecked = false;
            }
        }

        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == rbLightTheme && !isDarkTheme) return;
            if (sender == rbDarkTheme && isDarkTheme) return;

            isDarkTheme = rbDarkTheme.IsChecked == true;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (isDarkTheme)
            {
                // Dark theme
                this.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                this.Foreground = new SolidColorBrush(Colors.White);
                
                // Update checkbox and radio button text colors
                chkKeyboard.Foreground = new SolidColorBrush(Colors.White);
                chkMouse.Foreground = new SolidColorBrush(Colors.White);
                chkMouseMove.Foreground = new SolidColorBrush(Colors.White);
                chkSuperClean.Foreground = new SolidColorBrush(Color.FromRgb(255, 138, 101)); // Lighter orange for dark theme
                rbLightTheme.Foreground = new SolidColorBrush(Colors.White);
                rbDarkTheme.Foreground = new SolidColorBrush(Colors.White);
                
                // Update button colors if not running
                if (!isRunning)
                {
                    btnStartStop.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
            }
            else
            {
                // Light theme
                this.Background = new SolidColorBrush(Colors.White);
                this.Foreground = new SolidColorBrush(Colors.Black);
                
                // Update checkbox and radio button text colors
                chkKeyboard.Foreground = new SolidColorBrush(Colors.Black);
                chkMouse.Foreground = new SolidColorBrush(Colors.Black);
                chkMouseMove.Foreground = new SolidColorBrush(Colors.Black);
                chkSuperClean.Foreground = new SolidColorBrush(Color.FromRgb(255, 87, 34)); // Original orange for light theme
                rbLightTheme.Foreground = new SolidColorBrush(Colors.Black);
                rbDarkTheme.Foreground = new SolidColorBrush(Colors.Black);
                
                // Update button colors if not running
                if (!isRunning)
                {
                    btnStartStop.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (isRunning)
            {
                StopOptimization();
            }
            
            // Unregister hotkey
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
            
            // Dispose tray icon
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
            
            base.OnClosed(e);
        }
    }
}
