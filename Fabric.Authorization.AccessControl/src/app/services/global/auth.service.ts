import { Injectable } from '@angular/core';
import { Response } from '@angular/http';
import { HttpClient } from '@angular/common/http';
import { UserManager, User, Log } from 'oidc-client';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/catch';
import 'rxjs/add/operator/map';
import { ServicesService } from './services.service';
import { tap } from '../../../../node_modules/rxjs/operators';

export interface IAuthService {
  userManager: UserManager;
  identityClientSettings: any;
  clientId: string;
  authority: string;
  initialize(): Promise<any>;
  login();
  logout();
  handleSigninRedirectCallback();
  getUser(): Promise<User>;
  isUserAuthenticated(): Promise<boolean>;
}

@Injectable()
export class AuthService implements IAuthService {
  userManager: UserManager;
  identityClientSettings: any;
  clientId: string;
  authority: string;

  constructor(private httpClient: HttpClient, private servicesService: ServicesService) {
    this.clientId = 'fabric-access-control';
  }

  initialize(): Promise<any> {
    return this.servicesService.initialize().pipe(tap(url => {
      this.authority = url;
      const clientSettings: any = {
        authority: this.authority,
        client_id: this.clientId,
        redirect_uri: `${this.servicesService.accessControlEndpoint}/client/oidc-callback.html`,
        post_logout_redirect_uri: `${this.servicesService.accessControlEndpoint}/client/logged-out`,
        response_type: 'id_token token',
        scope: [
          'openid',
          'profile',
          'fabric.profile',
          'fabric/authorization.read',
          'fabric/authorization.write',
          'fabric/idprovider.searchusers',
          'fabric/authorization.dos.write'
        ].join(' '),
        silent_redirect_uri: `${this.servicesService.accessControlEndpoint}/client/silent.html`,
        automaticSilentRenew: true,
        filterProtocolClaims: true,
        loadUserInfo: true
      };

      this.userManager = new UserManager(clientSettings);

      this.userManager.events.addAccessTokenExpiring(function() {
        console.log('access token expiring');
      });

      this.userManager.events.addSilentRenewError(function(e) {
        console.log('silent renew error: ' + e.message);
      });

      this.userManager.events.addAccessTokenExpired(() => {
        console.log('access token expired');
        // when access token expires logout the user
        this.logout();
      });

      this.userManager.events.addUserSignedOut(() => {
        console.log('user logged out at the Idp, logging out');
        this.logout();
      });
    })).toPromise().then(() => console.log('finished initialize'));
  }

  login() {
    return this.userManager
      .signinRedirect();
  }

  logout() {
    this.userManager.signoutRedirect();
  }

  handleSigninRedirectCallback() {
    this.userManager
      .signinRedirectCallback()
      .then(user => {
        if (user) {
          console.log('Logged in: ' + JSON.stringify(user.profile));
        } else {
          console.log('could not log user in');
        }
      })
      .catch(e => {
        console.error(e);
      });
  }

  getUser(): Promise<User> {
    return this.userManager.getUser();
  }

  isUserAuthenticated(): Promise<boolean> {
    return this.userManager.getUser().then(function(user) {
      if (user) {
        return true;
      } else {
        return false;
      }
    });
  }

  private getAccessToken(): Promise<string> {
    return this.getUser().then(function(user) {
      if (user) {
        return Promise.resolve(user.access_token);
      }
    });
  }

  private handleError(error: Response | any) {
    Log.error('Error Response:');
    Log.error(error.message || error);
    return Observable.throw(error.message || error);
  }

  get<T>(resource: string): Promise<T> {
    return this.getAccessToken().then(token => {
      const requestUrl = this.authority + '/' + resource;
      return this.httpClient
        .get(requestUrl)
        .map((res: Response) => {
          return res.json();
        })
        .catch(error => this.handleError(error))
        .toPromise<T>();
    });
  }
}
