require('../resources/logo.svg');

import '../styles/index.scss';

import { Host } from 'mimo-website/core';
import { RootComponent } from 'mimo-website/components';
import HomeComponent from './home/homeComponent';

let host = new Host();
let container = host.getContainer();

container.bind<RootComponent>('RootComponent').to(HomeComponent).inSingletonScope();

host.run();