import React, {useEffect, useState} from 'react';
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";
import {getUser} from "../../../api/account";
import OwnerListingElement from "../listingPublic/OwnerListingElement";
import {getListing} from "../../../api/listing";
import ListingCard from "../shared/ListingCard";
import ListingCardHeader from "../shared/ListingCardHeader";
import ListingCardBody from "../shared/ListingCardBody";
import AvailableRolesSection from "../shared/AvailableRolesSection";
import {Listing} from "../../../types/Listing";

interface listingProfilePublicProps {
    listingId: string;
}

export const ListingProfilePublic: React.FC<listingProfilePublicProps> = ({listingId}) => {
    const [listing, setListing] = useState<Listing | null>(null);

    useEffect(() => {
        const fetchListing = async () => {
            const data = await getListing(listingId);
            if (data) {
                const owner = await getUser(data.ownerId);
                setListing({...data, owner});
            }
        };

        fetchListing();
    }, [listingId]);

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

    if (!listing) {
        return <div className="App-header">
            <MantineThemeProvider theme={theme}>
                <Loader size={200}/>
            </MantineThemeProvider>
        </div>;
    }

    return (
        <ListingCard>
            <ListingCardHeader
                ownerElement={<OwnerListingElement listing={listing}/>}
                title={listing.name}
                type={listing.type}
                genre={listing.genre}
            />
            <ListingCardBody description={listing.description}/>
            <AvailableRolesSection slots={listing.musicianSlots}/>
        </ListingCard>
    );
};
