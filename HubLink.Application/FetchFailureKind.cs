namespace HubLink.Application;

public enum FetchFailureKind
{
    ServiceUnreachable,   // server stopped, wrong address
    Timeout,              // no answer within TimeoutSeconds
    HttpError,            // 400, 500, etc.
    InvalidResponse       // 200 but body can't be parsed as shipments
}