import React from 'react';
import defaultProfileImage from '../../../assets/defaultProfileImage.jpg';
import './style.css';
import {createTheme, Loader, MantineThemeProvider} from "@mantine/core";
import {RingLoader} from "../../common/RingLoader";
import OwnerListingElement from "./OwnerListingElement";
import {formatMessageWithLinks} from "../../common/utils";
import {Listing} from "../../../types/Listing";

interface ListingPublicProps {
    listing: Listing;
}

const ListingPublic: React.FC<ListingPublicProps> = ({listing}) => {
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
        <div className={'listing'}>
            <div className={'listingHeader'}>
                <OwnerListingElement listing={listing}/>
                <div className={'listingTitle'}>
                    <p>{listing?.name}</p>
                </div>
                <div className={'listingType'}>
                    <div className={`listingType-${listing.type}`}>
                        <p>{listing.type === 'CollaborativeSong' ? 'Song' : listing.type}</p>
                    </div>
                    <p>{listing?.genre}</p>
                </div>
            </div>
            <div className={'listingBody'}>
                <p>{formatMessageWithLinks(listing?.description)}</p>
            </div>
            <div className={'listingFooter'}>
                {listing?.musicianSlots.map((slot: any) => (
                    <div key={slot.id}
                         className={`listingRole ${slot.status === 'Available' ? 'status-available' : 'status-filled'}`}>
                        <img src={defaultProfileImage} alt="Default Profile"/>
                        <p>Role: {slot.role}</p>
                        <p>Status: {slot.status}</p>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default ListingPublic;