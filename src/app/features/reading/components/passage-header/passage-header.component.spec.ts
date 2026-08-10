import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PassageHeaderComponent } from './passage-header.component';

describe('PassageHeaderComponent', () => {
  let component: PassageHeaderComponent;
  let fixture: ComponentFixture<PassageHeaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PassageHeaderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PassageHeaderComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
