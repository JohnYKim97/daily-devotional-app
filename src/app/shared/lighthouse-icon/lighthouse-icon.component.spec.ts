import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LighthouseIconComponent } from './lighthouse-icon.component';

describe('LighthouseIconComponent', () => {
  let component: LighthouseIconComponent;
  let fixture: ComponentFixture<LighthouseIconComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LighthouseIconComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(LighthouseIconComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
