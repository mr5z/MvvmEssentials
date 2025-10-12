namespace Nkraft.MvvmEssentials.Services.Navigation;

public interface IWindowEventHandler
{
	event EventHandler? Created;
	event EventHandler? Destroying;
	event EventHandler? Activated;
	event EventHandler? Deactivated;
	event EventHandler? Resumed;
	event EventHandler? Stopped;

	void OnCreated();
	void OnDestroying();
	void OnActivated();
	void OnDeactivated();
	void OnResumed();
	void OnStopped();
}

internal sealed class WindowEventHandler : IWindowEventHandler
{
	public event EventHandler? Created;
	public event EventHandler? Destroying;
	public event EventHandler? Activated;
	public event EventHandler? Deactivated;
	public event EventHandler? Stopped;
	public event EventHandler? Resumed;

	void IWindowEventHandler.OnActivated()
		=> Created?.Invoke(this, EventArgs.Empty);

	void IWindowEventHandler.OnDestroying()
		=> Destroying?.Invoke(this, EventArgs.Empty);

	void IWindowEventHandler.OnCreated()
		=> Activated?.Invoke(this, EventArgs.Empty);

	void IWindowEventHandler.OnDeactivated()
		=> Deactivated?.Invoke(this, EventArgs.Empty);

	void IWindowEventHandler.OnResumed()
		=> Resumed?.Invoke(this, EventArgs.Empty);

	void IWindowEventHandler.OnStopped()
		=> Stopped?.Invoke(this, EventArgs.Empty);
}