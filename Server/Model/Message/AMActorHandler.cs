﻿using System;

namespace Model
{
	public abstract class AMActorHandler<E, Message> : IMActorHandler where E : Entity where Message : AActorMessage
	{
		protected abstract void Run(E entity, Message message);

		public void Handle(Session session, Entity entity, object msg)
		{
			Message message = msg as Message;
			if (message == null)
			{
				Log.Error($"消息类型转换错误: {msg.GetType().FullName} to {typeof(Message).Name}");
			}
			E e = entity as E;
			if (e == null)
			{
				Log.Error($"Actor类型转换错误: {entity.GetType().Name} to {typeof(E).Name}");
			}
			this.Run(e, message);
		}

		public Type GetMessageType()
		{
			return typeof(Message);
		}
	}

	public abstract class AMActorRpcHandler<Request, Response> : IMActorHandler where Request : AActorRequest where Response : AActorResponse
	{
		protected static void ReplyError(Response response, Exception e, Action<Response> reply)
		{
			Log.Error(e.ToString());
			response.Error = ErrorCode.ERR_RpcFail;
			response.Message = e.ToString();
			reply(response);
		}

		protected abstract void Run(Entity entity, Request message, Action<Response> reply);

		public void Handle(Session session, Entity entity, object message)
		{
			try
			{
				Request request = message as Request;
				if (request == null)
				{
					Log.Error($"消息类型转换错误: {message.GetType().FullName} to {typeof(Request).Name}");
				}
				this.Run(session, request, response =>
				{
					// 等回调回来,session可以已经断开了,所以需要判断session id是否为0
					if (session.Id == 0)
					{
						return;
					}
					response.RpcId = request.RpcId;
					session.Reply(response);
				});
			}
			catch (Exception e)
			{
				throw new Exception($"解释消息失败: {message.GetType().FullName}", e);
			}
		}

		public Type GetMessageType()
		{
			return typeof(Request);
		}
	}
}