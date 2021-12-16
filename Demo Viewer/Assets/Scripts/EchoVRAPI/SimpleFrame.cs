namespace EchoVRAPI
{
	public class SimpleFrame
	{
		public string sessionid { get; set; }
		public bool private_match { get; set; }

		/// <summary>
		/// Name of the oculus username spectating.
		/// </summary>
		public string client_name { get; set; }
	}

}