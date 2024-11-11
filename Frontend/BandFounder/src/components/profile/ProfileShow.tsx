import React, { useEffect, useState } from 'react';
import {getAccount, getGUID, getTopArtists, getTopGenres} from './api';
import { Account } from "../../types/Account";

interface ProfileShowProps {
    username: string;
}

const ProfileShow: React.FC<ProfileShowProps> = ({ username }) => {
    const [guid, setGuid] = useState<string | undefined>(undefined);
    const [account, setAccount] = useState<Account | undefined>(undefined);
    const [topArtists, setTopArtists] = useState<string[] | undefined>([]);
    const [genres, setGenres] = useState<string[] | undefined>([]);

    useEffect(() => {
        const fetchGUID = async () => {
            const result = await getGUID(username);
            setGuid(result);
        };

        fetchGUID();
    }, [username]);

    useEffect(() => {
        const fetchAccount = async () => {
            if (guid) {
                const result = await getAccount(guid);
                setAccount(result);
            }
        };

        const fetchTopArtists = async () => {
            if (guid) {
                const result = await getTopArtists(guid);
                setTopArtists(result);
            }
        }

        const fetchGenres = async () => {
            if (guid) {
                const result = await getTopGenres(guid);
                setGenres(result);
            }
        }

        fetchGenres();
        fetchTopArtists();
        fetchAccount();
    }, [guid]);

    return (
        <div>
            <h1>Profile</h1>
            <img src={require('../../assets/defaultProfileImage.jpg')} alt="Default Profile"
                 style={{width: '100px', height: '100px', borderRadius: '25%'}}/>

            <div style={{display: 'flex', alignItems: 'center'}}>
                <p>Username: </p>
                <p style={{marginLeft: '8px'}}>{account?.name}</p>
            </div>
            <div style={{display: 'flex', alignItems: 'center'}}>
                <p>Guid: </p>
                <p style={{marginLeft: '8px'}}><i>{guid}</i></p>
            </div>
            <div style={{display: 'flex', alignItems: 'center'}}>
                <p>Email: </p>
                <p style={{marginLeft: '8px'}}>{account?.email}</p>
            </div>
            <div style={{display: 'flex'}}>
                <p>Top Artists: </p>
                <ul>
                    {topArtists?.map((artist, index) => (
                        <li key={index}>{artist}</li>
                    ))}
                </ul>
            </div>
            <div style={{display: 'flex'}}>
                <p>Genres: </p>
                <ul>
                    {genres?.map((genre, index) => (
                        <li key={index}>{genre}</li>
                    ))}
                </ul>
            </div>
        </div>
    );
};

export default ProfileShow;