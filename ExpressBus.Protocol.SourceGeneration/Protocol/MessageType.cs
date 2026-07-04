namespace ExpressBus.Protocol.SourceGeneration.Protocol;

/// <summary>
/// Mirror of <see cref="global::ExpressBus.Protocol.MessageType"/> used internally
/// by the source generator. Avoids a circular project reference since the generator
/// project cannot reference the Protocol project.
/// </summary>
internal enum MessageType
{
	Request,
	Response,
	Notification,
	Test,
}
