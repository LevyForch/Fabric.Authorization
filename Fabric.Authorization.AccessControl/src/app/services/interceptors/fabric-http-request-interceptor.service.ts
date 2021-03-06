
import {from as observableFrom,  Observable, empty } from 'rxjs';

import {mergeMap} from 'rxjs/operators';
import { Injectable, Inject } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent
} from '@angular/common/http';


import { IAuthService } from '../global/auth.service';
import { ServicesService } from '../global/services.service';

@Injectable()
export class FabricHttpRequestInterceptorService implements HttpInterceptor {
  protected static readonly AcceptHeader = 'application/json';
  protected static readonly ContentTypeHeader = 'application/json';
  protected static AuthorizationHeader = `Bearer`;

  constructor(@Inject('IAuthService')private authService: IAuthService, private servicesService: ServicesService) { }

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {

    if (this.servicesService.needsAuthToken(req.url)) {
      const tokenObservable = observableFrom(
        this.authService.getUser()
          .then(user => {
            if (user) {
              return user.access_token;
            }
          })
      );

      return tokenObservable.pipe(mergeMap(accessToken => {
        const modifiedRequest = req.clone({
          setHeaders: {
            Authorization: `${
              FabricHttpRequestInterceptorService.AuthorizationHeader
            } ${accessToken}`,
            'Accept': FabricHttpRequestInterceptorService.AcceptHeader,
            'Content-Type': FabricHttpRequestInterceptorService.ContentTypeHeader
          }
        });

        return next.handle(modifiedRequest);
      }));
    } else {
        const modifiedRequest = req.clone({
          setHeaders: {
            'Accept': FabricHttpRequestInterceptorService.AcceptHeader,
            'Content-Type': FabricHttpRequestInterceptorService.ContentTypeHeader
          }
        });

        return next.handle(modifiedRequest);
    }
  }
}
