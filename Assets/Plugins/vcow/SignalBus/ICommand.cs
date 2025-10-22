namespace SignalsSystem
{
	public interface ICommand
	{
		void InvokeCallback(object result);
	}
}