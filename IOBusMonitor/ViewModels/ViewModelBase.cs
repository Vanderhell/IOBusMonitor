using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IOBusMonitor
{
    /// <summary>
    /// Base class for all view-models.  
    /// Implements <see cref="INotifyPropertyChanged"/> and provides
    /// <c>OnPropertyChanged</c> / <c>SetProperty</c> helpers.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for the given property.
        /// </summary>
        /// <param name="propertyName">
        /// Name of the property. Automatically supplied by the compiler.
        /// </param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets <paramref name="field"/> to <paramref name="value"/> and raises
        /// <see cref="PropertyChanged"/> if the value actually changed.
        /// </summary>
        /// <typeparam name="T">Type of the underlying field.</typeparam>
        /// <param name="field">Reference to the backing field.</param>
        /// <param name="value">New value to assign.</param>
        /// <param name="propertyName">Name of the property (filled automatically).</param>
        /// <returns><c>true</c> if the value changed; otherwise <c>false</c>.</returns>
        protected bool SetProperty<T>(ref T field, T value,
                                      [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
