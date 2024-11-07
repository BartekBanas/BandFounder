import {FC} from "react";
import {useRoutes} from "react-router-dom";
import {MainPage} from "./MainPage";
import {SpotifyConnection} from "./SpotifyConnection";
import {LoginPage} from "./LoginPage";
import useAccountAuthorization from "../hooks/useAccountAuthorization";
import {RegisterPage} from "./RegisterPage";
import {ProfilePage} from "./ProfilePage";

const publicRoutes = [
    {
        path: "/",
        children: [
            {
                path: '/',
                element: <LoginPage/>
            },
            {
                path: '/register',
                element: <RegisterPage/>
            },
            {
                path: '*',
                element: <LoginPage/>
            }
        ]
    }
];

const privateRoutes = [{
    path: '/',
    children: [
        {
          path: '/',
            element: <MainPage/>
        },
        {
          path: '/home',
            element: <MainPage/>
        },
        {
          path: '/profile/:username',
          element: <ProfilePage/>
        },
        {
            path: '/spotifyConnection/callback/',
            element: <SpotifyConnection/>
        }
]
}]

export const Routing: FC = function () {
    const isAuthorized = useAccountAuthorization();
    let routes;
    if(isAuthorized){
        routes = privateRoutes;
    }
    else{
        routes = publicRoutes;
    }

    return useRoutes(routes);
};