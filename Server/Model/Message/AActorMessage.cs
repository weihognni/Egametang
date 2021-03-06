﻿namespace Model
{
	public abstract class AActorMessage: AMessage
	{
		public long Id { get; set; }
	}

	public abstract class AActorRequest : ARequest
	{
		public long Id { get; set; }
	}

	/// <summary>
	/// 服务端回的RPC消息需要继承这个抽象类
	/// </summary>
	public abstract class AActorResponse: AResponse
	{
	}
}