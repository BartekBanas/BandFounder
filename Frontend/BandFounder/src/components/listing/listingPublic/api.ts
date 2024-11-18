import Cookies from "universal-cookie";
import {API_URL} from "../../../config";

export const getListing = async (listingId: string) => {
    try{
        const jwt = new Cookies().get('auth_token');
        const response = await fetch(`${API_URL}/listings/${listingId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        return await response.json();
    }
    catch (e) {
        console.error('Error getting listing:', e);
    }
}

export const getListings = async (excludeOwn:boolean|undefined, matchMusicRole:boolean|undefined, sortFromLatest:boolean|undefined, showOnlyType:'CollaborativeSong'|'Band'|undefined, genre:string|undefined) => {
    try{
        const jwt = new Cookies().get('auth_token');
        let url = `${API_URL}/listings?`;
        if(excludeOwn !== undefined){
            url += `ExcludeOwn=${excludeOwn}&`;
        }
        if(matchMusicRole !== undefined){
            url += `MatchRole=${matchMusicRole}&`;
        }
        if(sortFromLatest !== undefined){
            url += `FromLatest=${sortFromLatest}&`;
        }
        if(showOnlyType !== undefined){
            url += `ListingType=${showOnlyType}&`;
        }
        if(genre !== undefined){
            url += `Genre=${genre}&`;
        }
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt}`
            }
        });
        console.log(response);
        return await response.json();
    }
    catch (e) {
        console.error('Error getting listings:', e);
    }
}