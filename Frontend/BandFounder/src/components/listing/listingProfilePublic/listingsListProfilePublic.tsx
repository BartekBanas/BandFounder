import React, {useEffect, useState} from 'react';
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";
import {ListingProfilePublic} from "./listingProfilePublic";
import {Listing} from "../../../types/Listing";
import {getUsersListings} from "../../../api/listing";
import {getAccountByUsername} from "../../../api/account";

interface ListingsListProfilePublicProps {
    profileUsername: string;
}

const ListingsListProfilePublic: React.FC<ListingsListProfilePublicProps> = ({profileUsername}) => {
    const [listings, setListings] = useState<Listing[]>([]);
    const [loading, setLoading] = useState<boolean>(true);

    useEffect(() => {
        const fetchListings = async () => {
            try {
                const user = await getAccountByUsername(profileUsername);
                const data = await getUsersListings(user.id);
                setListings(data);
            } catch (error) {
                console.error('Error getting listings:', error);
            }
            setLoading(false);
        };

        fetchListings();
    }, [profileUsername]);

    const theme = createTheme({
        components: {
            Loader: Loader.extend({
                defaultProps: {
                    loaders: {...Loader.defaultLoaders, ring: RingLoader},
                    type: 'ring',
                },
            }),
        },
    });

    if (loading) {
        return <div className="App-header">
            <MantineThemeProvider theme={theme}>
                <Loader size={200}/>
            </MantineThemeProvider>
        </div>;
    }

    return (
        <div className={'listingsList'}>
            {listings && listings.length > 0 ? (
                listings.map((listing) => (
                    <ListingProfilePublic key={listing.id} listingId={listing.id}/>
                ))
            ) : (
                <p>No listings available</p>
            )}
        </div>
    );
};

export default ListingsListProfilePublic;