import {fetchSpotifyTokens} from "../api/spotify";

async function useSpotifyAccountLinked(): Promise<boolean> {
    const spotifyTokens = await fetchSpotifyTokens();

    return !!spotifyTokens;
}

export default useSpotifyAccountLinked;