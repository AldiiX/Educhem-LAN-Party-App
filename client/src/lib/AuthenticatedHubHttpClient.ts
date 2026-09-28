import {DefaultHttpClient, HttpRequest, HttpResponse, ILogger} from "@microsoft/signalr";
import {AuthenticationRequiredError, ensureFreshAccessToken} from "@/lib/apiClient";

/**
 * pred kazdym vyjednanim spojeni zkontroluje expiraci, vcetne automatickyho reconnectu
 * samotny websocket zpravy pres tenhle klient nejdou
 */
export class AuthenticatedHubHttpClient extends DefaultHttpClient {
    constructor(logger: ILogger, private readonly onAuthenticationRequired: (error: AuthenticationRequiredError) => void) {
        super(logger);
    }

    override async send(request: HttpRequest): Promise<HttpResponse> {
        if(!request.url || !new URL(request.url, window.location.origin).pathname.endsWith("/negotiate")) {
            return super.send(request);
        }

        try {
            const refreshed = await ensureFreshAccessToken();
            let response = await super.send(request);
            // cookie mohla mezitim zmizet nebo klientsky hodiny nesedi, rozhoduje server
            if(response.statusCode === 401 && !refreshed) {
                await ensureFreshAccessToken(true);
                response = await super.send(request);
            }
            if(response.statusCode === 401) throw new AuthenticationRequiredError();
            return response;
        } catch(error) {
            if(error instanceof AuthenticationRequiredError) this.onAuthenticationRequired(error);
            throw error;
        }
    }
}
