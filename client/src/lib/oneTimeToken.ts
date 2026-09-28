export function takeOneTimeTokenFromUrl() {
    const url = new URL(window.location.href);
    const fragment = new URLSearchParams(url.hash.slice(1));
    // stary odkazy jeste muzou mit token v query; po nacteni ho hned uklidime
    const token = fragment.has("token") ? fragment.get("token") : url.searchParams.get("token");
    url.searchParams.delete("token");
    url.hash = "";
    window.history.replaceState(window.history.state, "", `${url.pathname}${url.search}`);
    return token ?? "";
}
