import React from 'react';
import './App.css';
import {Routing} from "./pages/Routing";
import {BrowserRouter} from "react-router-dom";

function App() {
  return (
      <BrowserRouter>
          <Routing/>
      </BrowserRouter>
  );
}

export default App;
