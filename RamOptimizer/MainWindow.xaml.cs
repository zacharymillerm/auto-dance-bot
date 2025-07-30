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

namespace RamOptimizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private InputSimulator inputSimulator;
        private Random random;
        private bool isRunning = false;
        private bool isDarkTheme = false;

        public MainWindow()
        {
            InitializeComponent();
            inputSimulator = new InputSimulator();
            random = new Random();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!isRunning) return;

            try
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
            catch (Exception ex)
            {
                txtStatus.Text = $"Error: {ex.Message}";
            }
        }

        private void SimulateKeyboardEvent()
        {
            // Randomly choose between Page Up and Page Down
            if (random.Next(2) == 0)
            {
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.PRIOR); // Page Up
                txtStatus.Text = "Sent: Page Up";
            }
            else
            {
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.NEXT); // Page Down
                txtStatus.Text = "Sent: Page Down";
            }
        }

        private void SimulateMouseEvent()
        {
            // Randomly choose between scroll up and scroll down
            int scrollAmount = random.Next(1, 4); // Random scroll amount between 1-3
            
            if (random.Next(2) == 0)
            {
                inputSimulator.Mouse.VerticalScroll(scrollAmount); // Scroll up
                txtStatus.Text = $"Sent: Scroll Up ({scrollAmount})";
            }
            else
            {
                inputSimulator.Mouse.VerticalScroll(-scrollAmount); // Scroll down
                txtStatus.Text = $"Sent: Scroll Down ({scrollAmount})";
            }
        }

        private void SimulateMouseMovement()
        {
            // Generate random movement within ±20px range
            int deltaX = random.Next(-20, 21); // -20 to +20
            int deltaY = random.Next(-20, 21); // -20 to +20
            
            // Move mouse to new position
            inputSimulator.Mouse.MoveMouseBy(deltaX, deltaY);
            
            txtStatus.Text = $"Mouse moved: ({deltaX}, {deltaY})";
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
                MessageBox.Show("Please enter a valid number for time range (minimum 10 seconds).", "Invalid Input", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (chkKeyboard.IsChecked != true && chkMouse.IsChecked != true && chkMouseMove.IsChecked != true)
            {
                MessageBox.Show("Please select at least one option (Keyboard, Mouse Scroll, or Mouse Movement).", "No Option Selected", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Start the timer
            timer.Interval = TimeSpan.FromSeconds(timeRange);
            timer.Start();
            isRunning = true;

            // Update UI
            btnStartStop.Content = "Stop";
            btnStartStop.Background = new SolidColorBrush(Colors.Gray);
            txtStatus.Text = "Optimization started...";
            
            // Disable controls
            chkKeyboard.IsEnabled = false;
            chkMouse.IsEnabled = false;
            chkMouseMove.IsEnabled = false;
            txtTimeRange.IsEnabled = false;
        }

        private void StopOptimization()
        {
            // Stop the timer
            timer.Stop();
            isRunning = false;

            // Update UI
            btnStartStop.Content = "Start";
            btnStartStop.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
            txtStatus.Text = "Optimization stopped.";

            // Enable controls
            chkKeyboard.IsEnabled = true;
            chkMouse.IsEnabled = true;
            chkMouseMove.IsEnabled = true;
            txtTimeRange.IsEnabled = true;
        }

        private void TxtTimeRange_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only numeric input
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
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
                
                // Update status text color
                txtStatus.Foreground = new SolidColorBrush(Colors.White);
                
                // Update checkbox and radio button text colors
                chkKeyboard.Foreground = new SolidColorBrush(Colors.White);
                chkMouse.Foreground = new SolidColorBrush(Colors.White);
                chkMouseMove.Foreground = new SolidColorBrush(Colors.White);
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
                
                // Update status text color
                txtStatus.Foreground = new SolidColorBrush(Colors.Gray);
                
                // Update checkbox and radio button text colors
                chkKeyboard.Foreground = new SolidColorBrush(Colors.Black);
                chkMouse.Foreground = new SolidColorBrush(Colors.Black);
                chkMouseMove.Foreground = new SolidColorBrush(Colors.Black);
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
            base.OnClosed(e);
        }
    }
}
