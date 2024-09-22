import {FC} from "react";
import {useRoutes} from "react-router-dom";
import {MainPage} from "./MainPage";
import {SpotifyConnection} from "./SpotifyConnection";

const publicRoutes = [
    {
        path: "/",
        children: [
            {
                path: '/',
                element: <MainPage/>
            },
            {
                path: '/spotifyConnection/*',
                element: <SpotifyConnection/>
            },
        ]
    }
];

const privateRoutes = [{}]

export const Routing: FC = function () {
    return useRoutes(publicRoutes);
};